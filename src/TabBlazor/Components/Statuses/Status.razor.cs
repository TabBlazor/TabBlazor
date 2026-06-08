using Microsoft.AspNetCore.Components;

namespace TabBlazor
{

    /// <summary>A status label/badge with an optional leading dot. Color comes from <see cref="TablerBaseComponent.BackgroundColor"/>.</summary>
    public partial class Status : TablerBaseComponent
    {
        /// <summary>When true, renders the lighter status style. Defaults to false.</summary>
        [Parameter] public bool Lite { get; set; }

        /// <summary>The leading dot style. Defaults to <see cref="StatusDotType.None"/>.</summary>
        [Parameter] public StatusDotType DotType { get; set; } = StatusDotType.None;


        protected override string ClassNames => ClassBuilder
            .Add("status")
            .Add(BackgroundColor.GetColorClass("status", ColorType.Default))
            .AddIf(TextColor.GetColorClass("text", ColorType.Default), TextColor!=TablerColor.Default)
            .AddIf("status-lite", Lite)
            .AddIf("cursor-pointer", OnClick.HasDelegate)

             



            .ToString();
    }

    /// <summary>The leading dot style for a <see cref="Status"/>.</summary>
    public enum StatusDotType
    {
        /// <summary>No dot.</summary>
        None = 0,
        /// <summary>A static dot.</summary>
        Normal = 1,
        /// <summary>An animated (pulsing) dot.</summary>
        Animate = 2
    }


}
