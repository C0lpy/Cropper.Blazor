﻿using System.Reflection;

namespace Cropper.Blazor.Client.Models
{
    // this is needed for the copy-to-clipboard feature
    public static partial class Snippets
    {
        public static string GetCode(string component)
        {
            FieldInfo? field = typeof(Snippets).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField)
                .FirstOrDefault(f => f.Name == component);

            if (field == null)
            {
                return $"Snippet for component '{component}' not found!";
            }

            return (string?)field.GetValue(null) ?? string.Empty;
        }

        public const string InstallScriptManual = @"<script src=""_content/Cropper.Blazor/cropper.min.js""></script>";

        public const string MinMaxZoomRatio_Script = @"
            window.overrideOnZoomCropperEvent = (minZoomRatio, maxZoomRatio) => {
                    window.cropper.onZoom = (imageObject, event, correlationId) => {
                        var jSEventData = this.getJSEventData(event, correlationId);

                        var isApplyPreventZoomMinRatio = (minZoomRatio != null) && (minZoomRatio > event.detail.ratio);
                        var isApplyPreventZoomMaxRatio = (maxZoomRatio != null) && (event.detail.ratio > maxZoomRatio);

                        if (isApplyPreventZoomMinRatio || isApplyPreventZoomMaxRatio) {
                            event.preventDefault();
                        }
                        else {
                            imageObject.invokeMethodAsync('CropperIsZoomed', jSEventData);
                        }
                    };
                };
            ";
    }
}
