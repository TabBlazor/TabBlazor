using Microsoft.AspNetCore.Components;

namespace TabBlazor
{

    public partial class Status : TablerBaseComponent
    {
        [Parameter] public bool Lite { get; set; }

        [Parameter] public StatusDotType DotType { get; set; } = StatusDotType.None;


        protected override string ClassNames => ClassBuilder
            .Add("status")
            .Add(BackgroundColor.GetColorClass("status", ColorType.Default))
            .AddIf(TextColor.GetColorClass("text", ColorType.Default), TextColor!=TablerColor.Default)
            .AddIf("status-lite", Lite)
            .AddIf("cursor-pointer", OnClick.HasDelegate)

             



            .ToString();
    }

    public enum StatusDotType
    {
        None = 0,
        Normal = 1,
        Animate = 2
    }


}
