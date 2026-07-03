# Custom Skill (voice intents)

The Smart Home skill (see [architecture.md](architecture.md)) maps HA entities to
Alexa devices you control with fixed verbs ("turn on", "set to 40%"). The
**Custom Skill** path is the complement: it lets you invoke an HA **script** by
voice and pass **parameters** in the utterance —

> "Alexa, ask lando to run the example routine at 40 percent"

where `40` is a slot forwarded to the script as a field.

It is a _second_ Alexa skill (Custom, not Smart Home) that shares the same AWS
Lambda, HMAC transport, account linking, and Home Assistant client as Smart
Home — only the lando route and handler differ.

---

## How it flows

```
Alexa Custom Skill  →  AWS Lambda (same one; branches on payload)
                    →  HMAC-signed POST  →  Azure Container App
                    →  POST /api/alexa/custom-skill  (AlexaIntent function)
                    →  IntentSkillHandler
                    →  IIntentScriptResolver  (intent name → HA script)
                    →  script.turn_on  with the mapped slot values as variables
```

- The Lambda detects a Custom Skill payload (top-level `request.type`) vs a Smart
  Home directive (top-level `directive`) and appends the per-skill segment to the
  single base `AZURE_ENDPOINT` — `/custom-skill` or `/smart-home` respectively
  (`src/aws/.../handler.ts`).
- `IntentSkillHandler` (`Lando.Alexa.CustomSkill`) is the intent-path analogue of
  `SmartHomeHandler`: same `IRequestHandler<TRequest,TResponse>` seam, different
  wire format. `AddRequestHandler` wires the shared HMAC validator automatically.

---

## The two layers you maintain

Unlike Smart Home (where `Discover` enumerates devices at runtime), a Custom
Skill's **interaction model is static** — Alexa must already know the intents,
slots, and sample utterances. So there are two layers:

1. **Interaction model** (Alexa side, static): intents + slot types + utterances,
   uploaded to the skill in the Alexa Developer Console (or via ASK CLI / SMAPI).
   See [`samples/alexa-custom-skill-model.json`](samples/alexa-custom-skill-model.json).
2. **Runtime routing** (lando side, dynamic): which HA script an intent runs and
   how its slots map to script fields. This is driven by HA entity attributes —
   no code change to add a new intent's _routing_.

`alexa_intent: true`-style flagging only governs layer 2. You still register the
utterance with Alexa in layer 1.

---

## Wiring an intent to a script (HA side)

Flag the script with two attributes (via `customize.yaml`):

```yaml
script.example_routine:
  lando_expose: true # must be exposed so lando's discovery sees it
  alexa_expose: false # keep it OUT of the Smart Home skill (optional)
  alexa_intent: RunRoutine # the Alexa intent name this script handles
  alexa_slots: # alexa slot name -> script field name
    level: level
    duration: duration
```

- `alexa_intent` is the **intent name** the script answers (string).
- `alexa_slots` maps each Alexa slot onto one of the script's `fields`. Slots not
  in the map are ignored; absent slots are simply not passed.
- The script receives the values as run variables (same as its declared
  `fields`), so `{{ level }}` / `{{ duration }}` resolve inside it.

> **Slot value ids that select a script.** When a custom slot's canonical value
> is used to pick _which_ entity to act on (e.g. a "routine" slot whose value
> names the target script), set each slot **value id** to that script's
> `object_id`. lando forwards the canonical id verbatim, so `script.{{ routine }}`
> resolves directly with no lookup table. Example: the interaction-model value
> `run the example routine` should carry `id: example_routine` to hit
> `script.example_routine`.

`lando_expose: true` + `alexa_expose: false` is the typical combination for an
**intent-only** script: lando can see it (so the resolver finds it) but it
doesn't also show up as a SceneController device in the Smart Home skill.

A full sample script lives at
[`samples/ha-intent-script.yaml`](samples/ha-intent-script.yaml).

> Slot transforms: built-in slots (`AMAZON.TIME`, `AMAZON.NUMBER`, `AMAZON.DATE`)
> arrive already normalized (`"06:00"`, `"40"`, `"2026-06-24"`); custom slot
> types resolve to their canonical value. The handler passes the canonical value
> (or the raw value when there's no resolution) straight through — do any further
> parsing in the script with Jinja.

---

## Deploying

1. **Create the Custom skill** in the Alexa Developer Console, upload the
   interaction model, and point its endpoint at the **same Lambda ARN** as the
   Smart Home skill.
2. **Account linking:** link the Custom skill to the _same_ Login-with-Amazon
   security profile as the Smart Home skill, so the existing bearer-token
   validation is reused.
3. **Invoke permission:** both the Smart Home directive trigger and the
   Custom Skill intent trigger are created unconditionally off the same
   Terraform variable, `alexa_skill_id` — they only differ in principal
   (`alexa-connectedhome.amazon.com` vs `alexa-appkit.amazon.com`). In the
   `cloud-city` superrepo this is the `alexa_skill_id` tfvars value, surfaced
   to CI as the `ALEXA_SKILL_ID` GitHub Actions variable (via
   `setup-github-secrets.sh`).
4. Deploy. No new env var is needed — the Lambda appends `/custom-skill` to the
   existing base `AZURE_ENDPOINT`, and lando already serves `/api/alexa/custom-skill`.

---

## Limitations / notes

- **Static model:** adding a _new intent_ (new utterance) still requires editing
  and re-uploading the interaction model. Only the script binding is dynamic. A
  future enhancement is generating the model JSON from the `alexa_intent`
  metadata and pushing it via SMAPI.
- **Slot prompting / dialogs** (Alexa asking "at what time?") is configured in the
  interaction model + Dialog directives — not yet handled here; one-shot intents
  only.
- The handler caches the intent→script map briefly (~60s) to avoid hitting HA on
  every utterance, so a newly-flagged script appears within a minute.
- "ask lando to …" uses the skill's **invocation name** ("lando"); the spoken
  result is a short confirmation ("Okay, running Example Routine.").
