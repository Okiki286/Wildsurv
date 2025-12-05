using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_2021_2_OR_NEWER
using System.IO;
#endif
using System.Threading.Tasks;
using UnityEngine;

namespace AssetInventory
{
    [Serializable]
    public sealed class HTMLExportStep : ActionStep
    {
        public HTMLExportStep()
        {
            Key = "HTMLExport";
            Name = "HTML Export";
            Description = "Export the full database to HTML using a template.";
            Category = ActionCategory.Actions;

            // Load available templates
            List<TemplateInfo> templates = TemplateUtils.LoadTemplates();

            // Template parameter
            List<Tuple<string, ParameterValue>> templateOptions = new List<Tuple<string, ParameterValue>>();
            for (int i = 0; i < templates.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(templates[i].name))
                {
                    templateOptions.Add(new Tuple<string, ParameterValue>(templates[i].name, new ParameterValue(i)));
                }
            }

            // Add a default option if no templates are available
            if (templateOptions.Count == 0)
            {
                templateOptions.Add(new Tuple<string, ParameterValue>("No templates available", new ParameterValue(-1)));
            }

            Parameters.Add(new StepParameter
            {
                Name = "Template",
                Description = "Template to use for the HTML export.",
                Type = StepParameter.ParamType.Int,
                ValueList = StepParameter.ValueType.Custom,
                Options = templateOptions,
                DefaultValue = templateOptions[0].Item2
            });

            // Target folder parameter
            Parameters.Add(new StepParameter
            {
                Name = "Target",
                Description = "Folder where the HTML export will be saved.",
                Type = StepParameter.ParamType.String,
                ValueList = StepParameter.ValueType.Folder,
                DefaultValue = new ParameterValue(AI.GetStorageFolder())
            });
        }

        public override async Task Run(List<ParameterValue> parameters)
        {
#if UNITY_2021_2_OR_NEWER
            // Get parameters
            int templateIndex = parameters[0].intValue;
            string targetFolder = parameters[1].stringValue;

            // Load templates fresh for the run (don't rely on constructor field)
            List<TemplateInfo> templates = TemplateUtils.LoadTemplates();

            // Check if templates are available
            if (templateIndex < 0)
            {
                Debug.LogError("No templates available for HTML export. Please ensure templates are present in the Templates folder.");
                return;
            }

            // Validate template index
            if (templateIndex >= templates.Count)
            {
                Debug.LogError($"Invalid template index: {templateIndex}");
                return;
            }

            // Skip empty separator entries
            if (string.IsNullOrWhiteSpace(templates[templateIndex].name))
            {
                Debug.LogError("Cannot export with an empty template name.");
                return;
            }

            // Create target folder if it doesn't exist
            Directory.CreateDirectory(targetFolder);

            // Load all assets from the database
            List<AssetInfo> assets = AI.LoadAssets()
                .Where(info => !info.Exclude)
                .Where(info => info.AssetSource != Asset.Source.RegistryPackage)
                .ToList();

            // Create export environment
            TemplateExportEnvironment env = new TemplateExportEnvironment
            {
                name = "Action Export",
                publishFolder = Path.GetFullPath(targetFolder),
                dataPath = "data/",
                imagePath = "Previews/",
                excludeImages = false,
                internalIdsOnly = false
            };

            // Get or create template export settings
            if (AI.Config.templateExportSettings == null)
            {
                AI.Config.templateExportSettings = new TemplateExportSettings();
            }

            if (AI.Config.templateExportSettings.environments == null || AI.Config.templateExportSettings.environments.Count == 0)
            {
                AI.Config.templateExportSettings.environments = new List<TemplateExportEnvironment> {env};
            }

            TemplateExport exporter = new TemplateExport();
            await exporter.Run(
                assets,
                templates[templateIndex],
                templates,
                AI.Config.templateExportSettings,
                env
            );
#else
            Debug.LogError("The HTML export step requires Unity 2021.2 or newer.");
            await Task.Yield();
#endif
        }
    }
}