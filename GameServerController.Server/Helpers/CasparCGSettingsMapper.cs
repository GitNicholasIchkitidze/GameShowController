using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.UI.Model;

namespace GameController.Server.Helpers
{
    public static class CasparCGSettingsMapper
    {
        public static Dictionary<CGTemplateEnums, templateSettingModel> MapSettings(CasparCGSettings? cgSettings)
        {
            var settingsMap = new Dictionary<CGTemplateEnums, templateSettingModel>();

            if (cgSettings != null)
            {
                foreach (CGTemplateEnums templateType in Enum.GetValues(typeof(CGTemplateEnums)))
                {
                    string templateName = templateType.ToString();

                    // მიიღეთ შესაბამისი property _cgSettings-დან reflection-ის მეშვეობით
                    var templateProperty = cgSettings.GetType().GetProperty(templateName);
                    if (templateProperty != null)
                    {
                        var templateData = templateProperty.GetValue(cgSettings);

                        if (templateData != null)
                        {
                            // Safely get property values and convert to correct types, handling nulls
                            string templateNameValue = templateData.GetType().GetProperty("TemplateName")?.GetValue(templateData) as string ?? string.Empty;
                            string templateUrlValue = templateData.GetType().GetProperty("TemplateUrl")?.GetValue(templateData) as string ?? string.Empty;
                            int channelValue = Convert.ToInt32(templateData.GetType().GetProperty("Channel")?.GetValue(templateData) ?? 0);
                            int layerValue = Convert.ToInt32(templateData.GetType().GetProperty("Layer")?.GetValue(templateData) ?? 0);
                            int layerCgValue = Convert.ToInt32(templateData.GetType().GetProperty("LayerCg")?.GetValue(templateData) ?? 0);
                            string serverIpValue = templateData.GetType().GetProperty("ServerIp")?.GetValue(templateData) as string ?? string.Empty;

                            settingsMap[templateType] = new templateSettingModel(
                                templateName,
                                templateNameValue,
                                templateUrlValue,
                                channelValue,
                                layerValue,
                                layerCgValue,
                                serverIpValue
                            );
                        }
                    }
                }
            }

            return settingsMap;
        }
    }
}
