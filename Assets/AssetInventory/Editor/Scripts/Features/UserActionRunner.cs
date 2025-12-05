using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetInventory
{
    public sealed class UserActionRunner : ActionProgress
    {
        public async Task Run(CustomAction ca)
        {
            // Get steps for this action
            List<CustomActionStep> steps = DBAdapter.DB.Query<CustomActionStep>("SELECT * FROM CustomActionStep WHERE ActionID = ? order by OrderIdx", ca.Id);

            // Check if we're resuming after a recompilation
            int lastExecutedStepIndex = EditorPrefs.GetInt(ActionHandler.AI_CURRENT_STEP + ca.Id, -1);
            bool isResuming = EditorPrefs.GetBool(ActionHandler.AI_ACTION_ACTIVE + ca.Id, false);

            if (isResuming)
            {
                if (AI.Config.LogCustomActions) Debug.Log($"Resuming custom action '{ca.Name}' after recompilation from step {lastExecutedStepIndex + 1}");
            }
            else
            {
                // mark that we're starting execution of this action
                EditorPrefs.SetBool(ActionHandler.AI_ACTION_ACTIVE + ca.Id, true);
                EditorPrefs.SetInt(ActionHandler.AI_CURRENT_STEP + ca.Id, -1);
                if (AI.Config.LogCustomActions) Debug.Log($"Starting fresh execution of custom action '{ca.Name}'");
            }

            MainCount = steps.Count;
            for (int i = 0; i < steps.Count; i++)
            {
                CustomActionStep step = steps[i];
                step.ResolveValues();
                if (step.StepDef == null)
                {
                    Debug.LogError($"Invalid action step definition. Step '{step.Key}' not found. Skipping.");
                    continue;
                }

                // skip steps that were already executed before recompilation
                if (isResuming && i <= lastExecutedStepIndex) continue;

                SetProgress(step.StepDef.Name, i + 1);
                if (AI.Config.LogCustomActions) Debug.Log($"Executing step {i + 1}/{steps.Count}: {step.StepDef.Name}");

                // validate parameters
                bool passed = true;
                for (int j = 0; j < step.StepDef.Parameters.Count; j++)
                {
                    StepParameter param = step.StepDef.Parameters[j];
                    if (param.Optional) continue;
                    if (
                        ((step.StepDef.GetParamType(param, step.Values) == StepParameter.ParamType.String || step.StepDef.GetParamType(param, step.Values) == StepParameter.ParamType.MultilineString) && string.IsNullOrWhiteSpace(step.Values[j].stringValue)) ||
                        (step.StepDef.GetParamType(param, step.Values) == StepParameter.ParamType.Int && step.Values[j].intValue == 0)
                    )
                    {
                        Debug.LogError($"Action step '{step.StepDef.Name}' is missing parameter '{param.Name}'.");
                        passed = false;
                    }
                }
                if (!passed) continue;

                // execute
                try
                {
                    // Mark this step as executed
                    EditorPrefs.SetInt(ActionHandler.AI_CURRENT_STEP + ca.Id, i);

                    await step.StepDef.Run(step.Values);
                    AssetDatabase.Refresh();

                    // wait for all processes to finish
                    while (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    {
                        await Task.Delay(25);
                    }
                    EditorPrefs.DeleteKey(ActionHandler.AI_ACTION_LOCK); // clear lock in case it was set by a step

                    if (AI.Config.LogCustomActions) Debug.Log($"Step {i + 1}/{steps.Count} completed successfully");
                    if (step.StepDef.InterruptsExecution) return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing custom action '{step.StepDef.Name}': {e.Message}");
                    Debug.LogException(e);
                }
            }
            if (AI.Config.LogCustomActions) Debug.Log($"Custom action '{ca.Name}' completed successfully");

            // clear execution state when done (either completed or failed)
            EditorPrefs.DeleteKey(ActionHandler.AI_ACTION_ACTIVE + ca.Id);
            EditorPrefs.DeleteKey(ActionHandler.AI_CURRENT_STEP + ca.Id);
        }
    }
}