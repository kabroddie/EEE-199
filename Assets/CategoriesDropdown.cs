using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CategoriesDropdown : MonoBehaviour
{
    [SerializeField] private GameObject categoryDropdownPrefab;
    [SerializeField] private Transform categoryListParent;
    [SerializeField] private TargetHandler targetHandler;

    private List<CategoriesHandler> activeDropdowns = new List<CategoriesHandler>();

    void Start()
    {
        List<string> categories = new List<string> {"Laboratory", "Office", "Classroom"};

        foreach (string category in categories)
        {
            List<TargetFacade> pois = targetHandler.CategoryPOIsNoFloorNo(category);
            GameObject dropdownGO = Instantiate(categoryDropdownPrefab, categoryListParent);

            var dropdown = dropdownGO.GetComponent<CategoriesHandler>();
            dropdown.Initialize(category, pois, targetHandler, this);

            activeDropdowns.Add(dropdown);
        }
    }

    public void CollapseAllExcept(CategoriesHandler current)
    {
        foreach (var dropdown in activeDropdowns)
        {
            if (dropdown != current)
            {
                dropdown.Collapse();
            }
        }
    }
}
