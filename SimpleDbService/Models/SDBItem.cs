using System.Collections.Generic;

namespace SimpleDbService.Models
{
    /// <summary>
    /// Model that represents single item for the SimpleDb store
    /// </summary>
    public class SdbItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SdbItem()
        {
            Attributes = new List<SdbItemAttribute>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="itemName">Unique name(id) of the item</param>
        /// <param name="attributes">SdbItem attributes</param>
        public SdbItem(string itemName, IEnumerable<SdbItemAttribute> attributes)
        {
            ItemName = itemName;
            Attributes = attributes;
        }

        /// <summary>
        /// Unique name of the item(ID)
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// SdbItem Attributes
        /// </summary>
        public IEnumerable<SdbItemAttribute> Attributes { get; set; }
    }
}
