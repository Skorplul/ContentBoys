﻿using System;
using ContentSettings.API.Attributes;
using ContentSettings.API.Settings;

namespace ContentBoys.Settings
{
    [SettingRegister("SKORP")]
    public class FreeShop : BoolSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            Configs.shopIsFree = Value;
        }

        protected override bool GetDefaultValue()
        {
            return false;
        }

        public string GetDisplayName() => "Free Shop";
    }

    [SettingRegister("SKORP")]
    public class InfinitStamina : BoolSetting, ICustomSetting
    {
        public override void ApplyValue()
        {
            Configs.infinitSprint = Value;
        }

        protected override bool GetDefaultValue()
        {
            return false;
        }

        public string GetDisplayName() => "Infinit Stamina";
    }
}