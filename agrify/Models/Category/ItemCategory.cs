namespace agrify.Models.Category;

public class ItemCategory
{
    public int ItemCategoryId { get; set; }
    public string Name { get; set; }

    // We can use this field to know if it's a
    // "Supply" category or "Produce" category, etc.
    public string CategoryType { get; set; }
}


