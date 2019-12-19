
namespace SimpleDbService.Models
{
    /// <summary>
    /// Model that represents the attribute of an item in SimpleDb store
    /// </summary>
    public class SdbItemAttribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SdbItemAttribute()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the attribute</param>
        /// <param name="value">Value of the attribute</param>
        public SdbItemAttribute(string name, string value) : this(name, value, true)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the attribute</param>
        /// <param name="value">Value of the attribute</param>
        /// <param name="replace">Flag that indicates whether the new value should be replaced or appended to the old value</param>
        public SdbItemAttribute(string name, string value, bool replace)
        {
            Name = name;
            Value = value;
            Replace = replace;
        }

        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Flag that indicates whether the new value should be replaced or appended to the old value
        /// </summary>
        public bool Replace { get; set; }
    }
}
