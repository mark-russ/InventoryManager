using System;
using System.Collections.Generic;
using eBay.Service.Core.Soap;

namespace Inventory_Manager.Objects
{
    public class eBayListingSchema
    {
        private FeatureDefinitionsType FeatureDefinitions { get; set; }
        private CategoryFeatureType FeatureValues { get; set; }

        public Dictionary<ListingTypeCodeType, List<eBayListingDuration>> ListingDurations { get; private set; } = new Dictionary<ListingTypeCodeType, List<eBayListingDuration>>();

        private void BuildListingDurations()
        {
            foreach (ListingDurationReferenceType reference in FeatureValues.ListingDuration)
            {
                foreach (ListingDurationDefinitionType definition in FeatureDefinitions.ListingDurations.ListingDuration)
                {
                    if (definition.durationSetID == reference.Value)
                    {
                        ListingDurations[reference.type] = new List<eBayListingDuration>(definition.Duration.Count);

                        foreach (String str in definition.Duration)
                        {
                            ListingDurations[reference.type].Add(new eBayListingDuration(str));
                        }

                        break;
                    }
                }
            }
        }

        public static eBayListingSchema GetSchema(String categoryId)
        {
            var apiCall = new eBay.Service.Call.GetCategoryFeaturesCall(App.eBayContextPRD)
            {
                CategoryID = categoryId,
                AllFeaturesForCategory = true,
                FeatureIDList = null,
                ViewAllNodes = true,
                DetailLevelList = new DetailLevelCodeTypeCollection(new DetailLevelCodeType[] { DetailLevelCodeType.ReturnAll })
            };

            var featureValues = apiCall.GetCategoryFeatures();

            eBayListingSchema schema = new eBayListingSchema()
            {
                FeatureDefinitions = apiCall.FeatureDefinitions,
                FeatureValues = featureValues[0]
            };

            schema.BuildListingDurations();
            return schema;
        }
    }

    public class eBayListingDuration : Adericium.ObservableObject
    {
        public eBayListingDuration(String value)
        {
            Value = value;
        }

        public String Display
        {
            get
            {
                return Value.Contains("_") ? Value.Substring(Value.LastIndexOf('_') + 1) + " days" : "Good 'Til Canceled";
            }
        }

        private String valuee;
        public String Value
        {
            get
            {
                return valuee;
            }
            set
            {
                valuee = value;
                NotifyPropertyChanged();
            }
        }
    }
}
