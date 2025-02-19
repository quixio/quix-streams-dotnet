﻿namespace QuixStreams.Streaming.Models
{

    /// <summary>
    /// Describes additional context for the parameter
    /// </summary>
    public class ParameterDefinition
    {
        /// <summary>
        /// Gets the unique parameter id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets the human friendly display name of the parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the description of the parameter
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets the location of the parameter within the Parameter hierarchy. Example: "/", "car/chassis/suspension".
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets the minimum value of the parameter
        /// </summary>
        public double? MinimumValue { get; set; }
        
        /// <summary>
        /// Gets the maximum value of the parameter
        /// </summary>
        public double? MaximumValue { get; set; }

        /// <summary>
        /// Gets the unit of the parameter 
        /// </summary>
        public string Unit { get;set; }

        /// <summary>
        /// Gets the formatting to apply on the value for display purposes
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets the optional field for any custom properties that do not exist on the parameter.
        /// For example this could be a json string, describing the optimal value range of this parameter
        /// </summary>
        public string CustomProperties { get; set; }

        /// <summary>
        /// Converts the Parameter definition to Telemetry layer structure
        /// </summary>
        /// <returns>Telemetry layer Parameter definition</returns>
        internal Telemetry.Models.ParameterDefinition ConvertToTelemetrysDefinition()
        {
            return new Telemetry.Models.ParameterDefinition 
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                MinimumValue = this.MinimumValue,
                MaximumValue = this.MaximumValue,
                Unit = this.Unit,
                Format = this.Format,
                CustomProperties = this.CustomProperties
            };
        }
    }
}