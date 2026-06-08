using Microsoft.AspNetCore.Components;

namespace TabBlazor
{

    /// <summary>A small status dot. Color comes from <see cref="TablerBaseComponent.BackgroundColor"/>.</summary>
    public partial class StatusDot : TablerBaseComponent
    {
        /// <summary>When true, the dot pulses. Defaults to false.</summary>
        [Parameter] public bool Animate { get; set; }


        protected override string ClassNames => ClassBuilder
            .Add("status-dot")
            .AddIf(BackgroundColor.GetColorClass("status", ColorType.Default), BackgroundColor != TablerColor.Default)
            .AddIf("status-dot-animated", Animate)
            .AddIf("cursor-pointer", OnClick.HasDelegate)
            .ToString();
    }
}
