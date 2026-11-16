# M01 candidate isolation checks

Source base `b6f71c1` plus this isolation slice, Unity 6000.6.0f1. All engine commands used this repository's explicit project path and ran sequentially. No other open Unity project was mutated. Login/Cloud check passed. No asset generation requests or paid generation jobs.

With `UNITY=/home/jbud/Unity/Hub/Editor/6000.6.0f1/Editor/Unity` and the repository working directory:

```sh
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.CandidateIsolationValidation.Run -logFile /tmp/siphon-candidate-isolation-reload3.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.FieldValidation.Run -logFile /tmp/siphon-isolation-field.log
"$UNITY" -batchmode -nographics -projectPath "$PWD" -executeMethod Siphon.Editor.PrototypeArtifacts.BuildLinuxMapCandidate -quit -logFile /tmp/siphon-map-candidate-build.log
xvfb-run -a -s '-screen 0 1280x720x24' Builds/SiphonMapCandidate600/Siphon.x86_64 -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -siphon-smoke -siphon-soak 25 -siphon-capture "$PWD/docs/images/candidate600.png" -siphon-capture-weapons -logFile /tmp/siphon-map-candidate-visible.log
```

Each final command exited 0. Default FieldValidation ran before the editor reload fix; the final targeted validator exercises that fix. Candidate generation hashes existing Assets/Siphon files and default EditorBuildSettings, proving they remain unchanged. Candidate checkpoint tests preserve/restore any existing candidate save, backup and temporary file. Six named accepted baseline hashes also match before/after in the [build record](M01-CANDIDATE-BUILD.json).

Two follow-up attempts failed before the final pass: Unity 6000.6 rejects obsolete GetInstanceID in the validator (replaced by GetEntityId); an assertion that an excluded scene has index -1 was wrong (observed index 2). The final test checks actual build-list exclusion and reload results. The initial reviewer correctly identified reliance on build indexing for an excluded scene as a risk, but its exact -1 claim was unverified. No remaining actionable findings in independent follow-up source review; the follow-up reviewer had no implementation role. A new reviewer thread could not be spawned due the thread limit, so an uninvolved existing reviewer handled that follow-up.

An initial batch-mode smoke also exited 0 (161 navigation searches, zero failures) but yielded black screenshots; those were discarded and the rendered smoke recaptured and visually inspected. Rendered run: 25s, 19 bots, four worker deliveries, 153 searches, zero failures, weapon selection/firing/recoil passed. Renderer llvmpipe LLVM23.1.3 on Xvfb; worst frame 2523ms including startup. These are smoke results, not accepted performance targets or large-map measurements. Existing movement tuning, weapon source and lab scene bytes unchanged; full movement validator not rerun.

Raw local logs: `artifacts/validation/candidate-isolation/` (ignored). Named build/source/output/screenshot hashes are committed in the build record. [Hosted trial 003](../reviews/trial-003.md) completed before merging PR #3: OCR covered seven selected C# files with zero findings; PR-Agent posted no major issues but pruned context. Source/docs/JSON whitespace check passed; Unity-generated YAML/meta retains its serializer whitespace and was not normalized, preserving tested hashes. No 2km route/seam/recovery/performance checks have been claimed.
