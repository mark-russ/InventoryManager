using System;
using System.Configuration;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Inventory_Manager.Objects
{
    public class Settings : ApplicationSettingsBase, INotifyPropertyChanged
    {
        [UserScopedSetting()]
        public ObservableCollection<Profile> Profiles
        {
            get
            {
                return (ObservableCollection<Profile>)this["Profiles"];
            }

            set
            {
                this["Profiles"] = value;
            }
        }

        [DefaultSettingValue("false"), UserScopedSetting()]
        public Boolean RememberProfile
        {
            get { return (Boolean)this["RememberProfile"]; }
            set { this["RememberProfile"] = value; base.OnPropertyChanged(this, new PropertyChangedEventArgs("RememberProfile")); }
        }
        
        [DefaultSettingValue("0"), UserScopedSetting()]
        public Int32 SelectedProfileIndex
        {
            get { return (Int32)this["SelectedProfileIndex"]; }
            set {
                if (value == -1)
                    value = 0;

                this["SelectedProfileIndex"] = value;
                base.OnPropertyChanged(this, new PropertyChangedEventArgs("SelectedProfileIndex"));
                base.OnPropertyChanged(this, new PropertyChangedEventArgs("Profile"));
            }
        }
        
        public Profile Profile
        {
            get
            {
                return Profiles[SelectedProfileIndex];
            }
        }
        
        [DefaultSettingValue("true"), UserScopedSetting()]
        public Boolean WindowPositionRemember
        {
            get
            {
                return (Boolean)this["WindowPositionRemember"];
            }
            set
            {
                this["WindowPositionRemember"] = value;
                base.OnPropertyChanged(this, new PropertyChangedEventArgs("WindowPositionRemember"));
            }
        }

        [UserScopedSetting()]
        public System.Windows.Point? WindowPosition
        {
            get
            {
                if (this["WindowPosition"] == null)
                    return null;

                return (System.Windows.Point)this["WindowPosition"];
            }
            set
            {
                this["WindowPosition"] = value;
            }
        }
        
        public Boolean FirstStart { get; set; }

        [DefaultSettingValue("v0.0.0"), UserScopedSetting()]
        public String Version
        {
            get {
                return (String)this["Version"];
            }
            private set {
                this["Version"] = value;
            }
        }
        
        public Settings() {
            SettingsLoaded += Settings_SettingsLoaded;
            SettingsSaving += Settings_SettingsSaving;
        }

        public void InvokeLoad() { if (this["Version"] == null) return; }

        private void Settings_SettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            // If no profiles exist, this is a new account.
            if (Profiles == null || Profiles.Count == 0)
            {
                var profile = new Profile() { Label = "Profile 1" };
                profile.UseDefaults();
                Profiles = new ObservableCollection<Profile>() { profile };
                SelectedProfileIndex = 0;
                FirstStart = true;
                return;
            }

            // If the last used profile is out of range, default to the last one profile in the list.
            if (SelectedProfileIndex > Profiles.Count - 1)
                SelectedProfileIndex = Profiles.Count - 1;
            
            App.eBayContextPRD = eBayAPIUtil.CreateContext(
                App.eBayAPIVersion,
                App.Settings.Profile.SiteCode,
                "Mark_Russ-MarkRuss-EasyLi-qqbbiz",
                "c265f9f3-d1a7-4bee-99a8-d0e38f3920cb",
                "MarkRuss-EasyList-PRD-781f16f17-f0d779bf",
                "PRD-81f16f173ac1-1ab8-4554-adb1-b294"
            );

            App.eBayContextSBX = eBayAPIUtil.CreateContext(
                App.eBayAPIVersion,
                App.Settings.Profile.SiteCode,
                "Mark_Russ-MarkRuss-EasyLi-oboda",
                "c265f9f3-d1a7-4bee-99a8-d0e38f3920cb",
                "MarkRuss-EasyList-SBX-c91c6ad5c-6423843e",
                "SBX-91c6ad5c796f-0c25-482a-a173-f17f"
            );
        }

        private void Settings_SettingsSaving(object sender, CancelEventArgs e) {
            Version = App.Version;
        }
    }
}
