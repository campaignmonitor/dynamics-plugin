using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Campmon.Dynamics.Utilities
{
    /// <summary>
    /// Utility to execute CRM Metadata Requests.
    /// </summary>
	public class MetadataService
    {
        private IOrganizationService _service;

        public MetadataService(IOrganizationService orgService)
        {
            if (orgService == null) { throw new ArgumentNullException("orgService"); }

            _service = orgService;
        }

        /// <summary>
        /// Get metadata for an attribute.
        /// </summary>
        /// <param name="entityName">Name of the entity containing the attribute.</param>
        /// <param name="attributeName">Attribute name to retrieve.</param>
        /// <returns>AttributeMetadata from a RetrieveAttributeResponse.</returns>
		public AttributeMetadata RetrieveAttribute(string entityName, string attributeName)
        {
            var request = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityName.ToLower(),
                LogicalName = attributeName.ToLower()
            };

            var response = (RetrieveAttributeResponse)_service.Execute(request);

            return response.AttributeMetadata;
        }

        /// <summary>
        /// Get metadata for an entity.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="entityFilter">Filter for the type of metadata to retrieve.</param>
        /// <returns>EntityMetadata from a RetrieveEntityResponse.</returns>
		public EntityMetadata RetrieveEntity(string entityName, EntityFilters entityFilter)
        {
            var request = new RetrieveEntityRequest
            {
                LogicalName = entityName.ToLower(),
                EntityFilters = entityFilter
            };

            var response = (RetrieveEntityResponse)_service.Execute(request);

            return response.EntityMetadata;
        }

        /// <summary>
        /// Get the integer type code for an entity by logical name.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <returns>The integer typecode for the entity.</returns>
		public int GetTypeCodeFromEntityName(string entityName)
        {
            var request = new RetrieveEntityRequest
            {
                LogicalName = entityName.ToLower(),
                EntityFilters = EntityFilters.Entity
            };

            var response = (RetrieveEntityResponse)_service.Execute(request);
            if (response != null && response.EntityMetadata.ObjectTypeCode.HasValue)
                return response.EntityMetadata.ObjectTypeCode.Value;

            throw new Exception("Could not find typecode for entity: " + entityName);
        }

        /// <summary>
        /// Get the logical name for an entity by typecode.
        /// </summary>
        /// <param name="typeCode">Integer typecode of the entity.</param>
        /// <returns>The string logical name for the entity.</returns>
		public string GetEntityNameFromTypeCode(int typeCode)
        {
            return GetEntityNameFromTypeCode(typeCode, false);
        }

        /// <summary>
        /// Get the logical name or display name for an entity by typecode.
        /// </summary>
        /// <param name="typeCode">Integer typecode of the entity.</param>
        /// <param name="displayName">True to return the display name, false to return the logical name.</param>
        /// <returns>The logical or display name of the entity.</returns>
		public string GetEntityNameFromTypeCode(int typeCode, bool displayName)
        {
            var request = new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity
            };

            var response = (RetrieveAllEntitiesResponse)_service.Execute(request);
            foreach (var entityMetadata in response.EntityMetadata)
            {
                if (entityMetadata.ObjectTypeCode.HasValue && entityMetadata.ObjectTypeCode.Value == typeCode)
                {
                    return displayName ? entityMetadata.DisplayName.UserLocalizedLabel.Label : entityMetadata.LogicalName;
                }
            }

            throw new ArgumentOutOfRangeException("typeCode", String.Format("No entity found of type code {0}", typeCode));
        }

        /// <summary>
        /// Get the valid status options for a given entity state.
        /// </summary>
        /// <param name="entity">Logical name of the entity.</param>
        /// <param name="state">Integer value of the entity state.</param>
        /// <returns>List of OptionMetadata with each valid status option.</returns>
		public List<OptionMetadata> GetStatusOptionsForEntityByState(string entity, int state)
        {
            var attribute = (StatusAttributeMetadata)RetrieveAttribute(entity, "statuscode");

            var options = new List<OptionMetadata>();
            foreach (var option in attribute.OptionSet.Options)
            {
                if (option.Value.HasValue && option.Value.Value == state)
                    options.Add(option);
            }

            return options;
        }

        /// <summary>
        /// Get state option that corresponds to an entity's status value.
        /// </summary>
        /// <param name="entity">Logical name of the entity.</param>
        /// <param name="status">Integer value of the entity status.</param>
        /// <returns>OptionMetadata for the entity state.</returns>
		public OptionMetadata GetCorrespondingStateOfStatus(string entity, int status)
        {
            var statusMetadata = (StatusAttributeMetadata)RetrieveAttribute(entity, "statuscode");
            var stateMetadata = (StateAttributeMetadata)RetrieveAttribute(entity, "statecode");

            //TODO: Clean this up
            foreach (var option in statusMetadata.OptionSet.Options)
            {
                if (option.Value.HasValue && option.Value.Value == status)
                    foreach (var stateOption in stateMetadata.OptionSet.Options)
                        if (stateOption.Value.HasValue && stateOption.Value.Value == option.Value.Value)
                            return stateOption;
            }

            return null;
        }

        /// <summary>
        /// Gets the name of the primary attribute for an entity.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <returns>Logical name of the PrimaryNameAttribute for the requested entity.</returns>
		public String GetPrimaryAttributeName(String entityName)
        {
            return RetrieveEntity(entityName, EntityFilters.Entity).PrimaryNameAttribute;
        }

        /// <summary>
        /// Gets the integer value for an entity's statuscode by name.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="statuscodeName">Name of the statuscode.</param>
        /// <returns>Integer value for the statuscode.</returns>
		public int GetIntValueFromStatusString(String entityName, String statuscodeName)
        {
            return GetIntValueFromPicklistString(entityName, "statuscode", statuscodeName);
        }

        /// <summary>
        /// Gets the integer value for an entity's statecode.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="statecodeName">Name of the statecode.</param>
        /// <returns>Integer value for the statecode.</returns>
		public int GetIntValueFromStateString(String entityName, String statecodeName)
        {
            return GetIntValueFromPicklistString(entityName, "statecode", statecodeName);
        }

        /// <summary>
        /// Gets the integer value for a picklist option by name.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="picklistValue">Name of the picklist option.</param>
        /// <returns>Integer value for the picklist option.</returns>
		public int GetIntValueFromPicklistString(String entityName, String attributeName, String picklistValue)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var type = attributeMetadata.GetType();
            var options = new OptionMetadataCollection();

            if (type == typeof(PicklistAttributeMetadata))
                options = ((PicklistAttributeMetadata)attributeMetadata).OptionSet.Options;
            else if (type == typeof(StateAttributeMetadata))
                options = ((StateAttributeMetadata)attributeMetadata).OptionSet.Options;
            else if (type == typeof(StatusAttributeMetadata))
                options = ((StatusAttributeMetadata)attributeMetadata).OptionSet.Options;

            foreach (var option in options)
            {
                if (option.Value.HasValue && option.Label.UserLocalizedLabel.Label.ToLower() == picklistValue.ToLower())
                    return option.Value.Value;
            }

            return -1;
        }

        /// <summary>
        /// Gets the integer value for a picklist option by name and language code.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="picklistValue">Name of the picklist option.</param>
        /// <param name="languageCode">Language code of the picklist option.</param>
        /// <returns>Integer value for the picklist option.</returns>
        public int GetIntValueFromPicklistString(String entityName, String attributeName, String picklistValue, int languageCode)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var type = attributeMetadata.GetType();
            var options = new OptionMetadataCollection();

            if (type == typeof(PicklistAttributeMetadata))
                options = ((PicklistAttributeMetadata)attributeMetadata).OptionSet.Options;
            else if (type == typeof(StateAttributeMetadata))
                options = ((StateAttributeMetadata)attributeMetadata).OptionSet.Options;
            else if (type == typeof(StatusAttributeMetadata))
                options = ((StatusAttributeMetadata)attributeMetadata).OptionSet.Options;


            foreach (var option in options)
            {
                if (option.Value.HasValue)
                {
                    var label = option.Label.LocalizedLabels.FirstOrDefault(
                        x => x.LanguageCode == languageCode && picklistValue.Equals(x.Label, StringComparison.CurrentCultureIgnoreCase));
                    if (label != null)
                    {
                        return option.Value.Value;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the label for a picklist option integer value.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="picklistValue">Integer value of the picklist option.</param>
        /// <returns>User-localized label for the picklist option.</returns>
		public String GetStringValueFromPicklistInt(String entityName, String attributeName, int picklistValue)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var picklistMetadata = attributeMetadata as PicklistAttributeMetadata;

            if (picklistMetadata != null)
            {
                foreach (var option in picklistMetadata.OptionSet.Options)
                    if (option.Value.HasValue && option.Value.Value == picklistValue)
                        return option.Label.UserLocalizedLabel.Label;
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the label for a picklist option integer value and country code.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="picklistValue">Integer value of the picklist option.</param>
        /// <param name="languageCode">Integer language code of the picklist option.</param>
        /// <returns>Localized label for the picklist option.</returns>
        public String GetStringValueFromPicklistInt(String entityName, string attributeName, int picklistValue, int languageCode)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var picklistMetadata = attributeMetadata as PicklistAttributeMetadata;

            if (picklistMetadata != null)
            {
                var option = picklistMetadata.OptionSet.Options.FirstOrDefault(x => x.Value == picklistValue);
                if (option != null)
                {
                    return option.Label.LocalizedLabels.FirstOrDefault(x => x.LanguageCode == languageCode).Label ?? String.Empty;
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the label for a boolean attribute value.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="booleanValue">Boolean value of the attribute.</param>
        /// <returns>User-localized label for the boolean value. </returns>
        public String GetStringValueFromBooleanValue(String entityName, String attributeName, bool booleanValue)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var booleanMetadata = attributeMetadata as BooleanAttributeMetadata;

            if (booleanMetadata != null)
            {
                return booleanValue ?
                    booleanMetadata.OptionSet.TrueOption.Label.UserLocalizedLabel.Label :
                    booleanMetadata.OptionSet.FalseOption.Label.UserLocalizedLabel.Label;
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the label for a boolean attribute value and language code.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="booleanValue">Boolean value of the attribute.</param>
        /// <param name="languageCode">Integer language code of the label.</param>
        /// <returns>Localized label for the boolean value. </returns>
        public String GetStringValueFromBooleanValue(String entityName, string attributeName, bool booleanValue, int languageCode)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var booleanMetadata = attributeMetadata as BooleanAttributeMetadata;

            if (booleanMetadata != null)
            {
                var option = booleanValue ? booleanMetadata.OptionSet.TrueOption : booleanMetadata.OptionSet.FalseOption;

                if (option != null)
                {
                    return option.Label.LocalizedLabels.FirstOrDefault(x => x.LanguageCode == languageCode).Label ?? String.Empty;
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the label for an entity's integer statecode.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <param name="statusCode">Integer value of the statuscode</param>
        /// <returns>User-localized label of the statuscode.</returns>
        public string GetStringValueFromStatusInt(string entityName, string attributeName, int statusCode)
        {
            var attributeMetadata = RetrieveAttribute(entityName, attributeName);
            var statusMetadata = attributeMetadata as StatusAttributeMetadata;

            if (statusMetadata != null)
            {
                foreach (var option in statusMetadata.OptionSet.Options)
                    if (option.Value.HasValue && option.Value.Value == statusCode)
                        return option.Label.UserLocalizedLabel.Label;
            }

            return String.Empty;
        }

        /// <summary>
        /// Determine if an attribute exists on a given entity.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <returns>True/False if the attribute is present on the entity.</returns>
		public bool DoesAttributeExistOnEntity(string entityName, string attributeName)
        {
            try
            {
                RetrieveAttribute(entityName, attributeName);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the display name for an entity.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <returns>Display name of the entity.</returns>
		public String GetDisplayNameFromEntity(string entityName)
        {
            var em = RetrieveEntity(entityName, EntityFilters.Entity);
            if (em != null)
            {
                return em.DisplayName.UserLocalizedLabel.Label;
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the display name for an attribute.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <param name="attributeName">Logical name of the attribute.</param>
        /// <returns>Display name of the attribute.</returns>
		public String GetDisplayNameFromAttribute(string entityName, string attributeName)
        {
            var am = RetrieveAttribute(entityName, attributeName);
            if (am != null)
            {
                return am.DisplayName.UserLocalizedLabel.Label;
            }

            return String.Empty;
        }

        /// <summary>
        /// Get the name of an entity's primary key attribute.
        /// </summary>
        /// <param name="entityName">Logical name of the entity.</param>
        /// <returns>Logical name of the entity's primary key.</returns>
		public String GetPrimaryKeyName(string entityName)
        {
            var entityMetadata = RetrieveEntity(entityName, EntityFilters.Entity);
            if (entityMetadata != null)
            {
                return entityMetadata.PrimaryIdAttribute;
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets the metadata for a global option set.
        /// </summary>
        /// <param name="globalOptionSetName">Logical name of the option set.</param>
        /// <returns>List of OptionMetadata for the option set.</returns>
        public List<OptionMetadata> RetrieveGlobalOptionSet(string globalOptionSetName)
        {
            var retrieveOptionSetRequest = new RetrieveOptionSetRequest
            { Name = globalOptionSetName };

            var retrieveOptionSetResponse = (RetrieveOptionSetResponse)
                _service.Execute(retrieveOptionSetRequest);

            var retrievedOptionSetMetadata = (OptionSetMetadata)
                retrieveOptionSetResponse.OptionSetMetadata;

            return new List<OptionMetadata>(retrievedOptionSetMetadata.Options.ToArray());
        }
    }
}