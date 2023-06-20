using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Inventory_Manager.Objects
{
    public enum NamingModes
    {
        [Description("Verbatim: No prefixing or suffixing")]
        Verbatim,
        [Description("Prefix the name with the extra field")]
        Prefix,
        [Description("Suffix the name with the extra field")]
        Suffix
    }

    public enum ExtraBehaviors
    {
        [Description("Inactive: Purely for information")]
        Inactive,
        [Description("Dates: Dates and date ranges")]
        Dates
    }

    public class ItemCategory : Adericium.ObservableObject
    {
        private String label;
        public String Label {
            get { return label; }
            set { label = value; NotifyPropertyChanged(); }
        }

        private String descriptionTemplate = String.Empty;
        public String DescriptionTemplate {
            get { return descriptionTemplate; }
            set { descriptionTemplate = value; NotifyPropertyChanged(); }
        }

        private NamingModes namingMode = NamingModes.Verbatim;
        public NamingModes NamingMode {
            get { return namingMode; }
            set { namingMode = value; NotifyPropertyChanged(); }
        }

        private ExtraBehaviors extraBehavior = ExtraBehaviors.Inactive;
        public ExtraBehaviors ExtraBehavior {
            get { return extraBehavior; }
            set { extraBehavior = value; NotifyPropertyChanged(); }
        }

        private Boolean deleteVariations = false;
        public Boolean DeleteVariations {
            get { return deleteVariations; }
            set { deleteVariations = value; NotifyPropertyChanged(); }
        }

        private Boolean soldOutClearExtra = false;
        public Boolean SoldOutClearExtra {
            get { return soldOutClearExtra; }
            set { soldOutClearExtra = value; NotifyPropertyChanged(); }
        }

        private Boolean generateDescriptionVariants = false;
        public Boolean GenerateDescriptionVariants {
            get { return generateDescriptionVariants; }
            set { generateDescriptionVariants = value; NotifyPropertyChanged(); }
        }
    }
}
