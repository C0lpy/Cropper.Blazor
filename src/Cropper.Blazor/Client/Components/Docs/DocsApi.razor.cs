﻿using System.Globalization;
using System.Reflection;
using Cropper.Blazor.Client.Models;
using Cropper.Blazor.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Cropper.Blazor.Client.Components.Docs
{
    public partial class DocsApi
    {
        private List<string> hiddenMethods = new List<string>()
        {
            "ToString",
            "GetType",
            "GetHashCode",
            "Equals",
            "SetParametersAsync",
            "ReferenceEquals"
        };

        [Parameter] public Type Type { get; set; }
        [Parameter] public bool IsContract { get; set; } = false;

        private IEnumerable<ApiProperty> GetEventCallbacks()
        {
            string saveTypename = DocStrings.GetSaveTypename(Type);

            if (IsContract)
            {
                yield break;
            }
            else
            {
                foreach (var info in Type.GetPropertyInfosWithAttribute<ParameterAttribute>().OrderBy(x => x.Name))
                {
                    if (info.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).Length == 0
                        && IsEventCallback(info))
                    {
                        yield return new ApiProperty
                        {
                            Name = info.Name,
                            PropertyInfo = info,
                            Default = string.Empty,
                            Description = DocStrings.GetMemberDescription(saveTypename, info),
                            IsTwoWay = CheckIsTwoWayEventCallback(info),
                            Type = info.PropertyType,
                        };
                    }
                }
            }
        }

        private IEnumerable<ApiMethod> GetMethods()
        {
            string saveTypename = DocStrings.GetSaveTypename(Type);

            if (IsContract)
            {
                yield break;
            }
            else
            {
                foreach (var info in Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Static).OrderBy(x => x.Name))
                {
                    if (!hiddenMethods.Any(x => x.Contains(info.Name)) && !info.Name.StartsWith("get_") && !info.Name.StartsWith("set_"))
                    {
                        if (info.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0
                            && info.GetCustomAttributes(typeof(JSInvokableAttribute), true).Length == 0)
                        {
                            yield return new ApiMethod()
                            {
                                MethodInfo = info,
                                Return = info.ReturnParameter,
                                Signature = info.GetSignature(),
                                Parameters = info.GetParameters(),
                                Documentation = DocStrings.GetMemberDescription(saveTypename, info)
                            };
                        }
                    }
                }
            }
        }

        private bool IsEventCallback(PropertyInfo? propertyInfo)
        {
            return propertyInfo!.PropertyType.Name.Contains("EventCallback")
                || propertyInfo!.PropertyType.Name.Contains("Action")
                || propertyInfo!.PropertyType.Name.Contains("Func");
        }

        private IEnumerable<ApiProperty> GetProperties()
        {
            string saveTypename = DocStrings.GetSaveTypename(Type);
            IEnumerable<PropertyInfo> types = null!;

            if (IsContract)
            {
                types = Type
                    .GetPropertyInfos();
            }
            else
            {
                types = Type
                    .GetPropertyInfosWithAttribute<ParameterAttribute>();
            }

            foreach (var info in types.OrderBy(x => x.Name))
            {
                if (info.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0
                    && !IsEventCallback(info))
                {
                    yield return ToApiProperty(info, saveTypename);
                }
            }
        }

        private ApiProperty ToApiProperty(PropertyInfo info, string saveTypename)
        {
            return new ApiProperty
            {
                Name = info.Name,
                PropertyInfo = info,
                Default = GetDefaultValue(info),
                IsTwoWay = CheckIsTwoWayProperty(info),
                Description = DocStrings.GetMemberDescription(saveTypename, info, IsContract),
                Type = info.PropertyType
            };
        }

        private string AnalyseMethodDocumentation(string documentation, string occurrence, string parameter = "")
        {
            try
            {
                // Define local variable
                string doublequotes = @"""";

                // Define the start tag and the end tag
                string endTag = $"</{occurrence}>";
                string startTag = $"<{occurrence}{(parameter == string.Empty ? "" : " name=" + doublequotes + parameter + doublequotes)}>";

                // Check if the documentation is valid and contains the start tag
                if (documentation != null && documentation.Contains(startTag))
                {
                    // Remove the beginning of the documentation until the start tag
                    documentation = documentation.Substring(documentation.IndexOf(startTag), documentation.Length - documentation.IndexOf(startTag));

                    // Check if the documentation contains the end tag
                    if (documentation.Contains(endTag))
                    {
                        // Return the extracted information
                        // If the information is not for summary, ' : ' is only added if there is a non-empty information to be returned
                        return ((occurrence != "summary" && documentation.Substring(startTag.Length, documentation.IndexOf(endTag) - startTag.Length).Trim() != "" ? "" : "") +
                            documentation.Substring(startTag.Length, documentation.IndexOf(endTag) - startTag.Length).Trim())
                            .Replace("&gt;", ">")
                            .Replace("&lt;", "<");
                    }
                }
            }
            catch
            {
                // ignored
            }

            return string.Empty;
        }

        private bool CheckIsTwoWayEventCallback(PropertyInfo propertyInfo) => propertyInfo.Name.EndsWith("Changed") ? true : false;

        private bool CheckIsTwoWayProperty(PropertyInfo propertyInfo)
        {
            PropertyInfo eventCallbackInfo = Type.GetProperty(propertyInfo.Name + "Changed");

            return eventCallbackInfo != null &&
                   eventCallbackInfo.PropertyType.Name.Contains("EventCallback") &&
                   eventCallbackInfo.GetCustomAttribute<ParameterAttribute>() != null &&
                   eventCallbackInfo.GetCustomAttribute<ObsoleteAttribute>() == null;
        }

        // used for default value getting
        object _comp_instance;

        RenderFragment RenderTheType()
        {
            if (!Type.IsAssignableTo(typeof(IComponent)))
                return null;
            return new RenderFragment(builder =>
            {
                builder.OpenComponent(0, Type);
                builder.AddComponentReferenceCapture(1, inst => { _comp_instance = inst; });
                builder.CloseComponent();
            });
        }

        private object GetDefaultValue(PropertyInfo info)
        {
            try
            {
                if (_comp_instance == null)
                {
                    return null;
                    //RenderTheType();
                }
                return info.GetValue(_comp_instance);
            }
            catch (Exception) { }
            return null;
        }

        DefaultConverter<object> _converter = new DefaultConverter<object>()
        {
            Culture = CultureInfo.InvariantCulture
        };

        private string PresentDefaultValue(object value)
        {
            if (value == null)
                return "null";
            if (value.GetType() == typeof(string))
            {
                if (value.ToString() == string.Empty)
                {
                    return "";
                }
                else
                {
                    return $"\"{value}\"";
                }
            }
            if (value.GetType().IsEnum)
                return $"{value.GetType().Name}.{value}";
            if (Nullable.GetUnderlyingType(value.GetType()) != null)
                return _converter.Set(value);
            if (value.GetType().IsGenericType) // for instance event callbacks
                return "";
            if (value.GetType().IsValueType)
                return _converter.Set(value);
            return "";
        }


        #region Grouping properties

        private enum Grouping { Categories, Inheritance, None }

        private Grouping _propertiesGrouping = Grouping.None;

        private TableGroupDefinition<ApiProperty> PropertiesGroupDefinition => _propertiesGrouping switch
        {
            Grouping.Categories => new() { Selector = (p) => p.PropertyInfo.GetCustomAttribute<CategoryAttribute>()?.Name ?? "Misc" },
            Grouping.Inheritance => new() { Selector = (p) => BaseDefinitionClass(p.PropertyInfo) },
            _ => null
        };

        // -- Grouping properties by inheritance ------------------------------------------------------------------------------------------

        private static Type BaseDefinitionClass(MethodInfo m) => m.GetBaseDefinition().DeclaringType;

        private static Type BaseDefinitionClass(PropertyInfo p) => BaseDefinitionClass(p.GetMethod ?? p.SetMethod);  // used for grouping properties

        private static bool IsOverridden(MethodInfo m) => m.GetBaseDefinition().DeclaringType != m.DeclaringType;

        private static bool IsOverridden(PropertyInfo p) => IsOverridden(p.GetMethod ?? p.SetMethod);                // used for the "overridden" chip

        // used for ordering groups of properties
        private static int NumberOfAncestorClasses(Type type)
        {
            int n = 0;
            while ((type = type.BaseType) != null)
                n++;
            return n;
        }

        #endregion
    }
}
