﻿using Microsoft.JSInterop;

namespace Cropper.Blazor.Events.CropEvent
{
    /// <summary>
    /// Provides the metadata of a Crop JS Event.
    /// </summary>
    public class CropJSEvent : BaseJSEvent
    {
        /// <summary>
        /// Implementation of the constructor.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
        /// <param name="jsRuntimeObjectRef">The <see cref="IJSObjectReference"/>.</param>
        public CropJSEvent(IJSRuntime jsRuntime, IJSObjectReference jsRuntimeObjectRef)
            : base(jsRuntime, jsRuntimeObjectRef)
        {
        }

        /// <summary>
        /// Represents a Crop JavaScript Event object.
        /// </summary>
        public JSEventData<CropEvent> EventData { get; internal set; } = null!;
    }
}
