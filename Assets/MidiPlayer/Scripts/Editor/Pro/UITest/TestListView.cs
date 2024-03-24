#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class ListViewExample : EditorWindow
{
    // Classe pour repr�senter vos donn�es
    public class CharacterInfo
    {
        public string name;
        public int maxHp;
        public int currentHp;
    }

    //[MenuItem("MaestroTest/ListView Custom Item")]
    public static void OpenWindow()
    {
        GetWindow<ListViewExample>().Show();
    }

    private void OnEnable()
    {
        // Cr�ez et remplissez la liste des objets CharacterInfo
        const int itemCount = 50;
        List<CharacterInfo> items = new List<CharacterInfo>(itemCount);
        for (int i = 1; i <= itemCount; i++)
        {
            CharacterInfo character = new CharacterInfo
            {
                name = $"Character {i}",
                maxHp = 100
            };
            character.currentHp = character.maxHp;
            items.Add(character);
        }

        // La ListView appelle ceci pour ajouter des �l�ments visibles au d�filement
        Func<VisualElement> makeItem = () => new Label();

        // La ListView appelle ceci si un nouvel �l�ment devient visible
        Action<VisualElement, int> bindItem = (e, i) =>
        {
            (e as Label).text = items[i].name;
        };

        // Utilisez le constructeur avec des valeurs initiales pour cr�er la ListView
        ListView listView = new ListView(items, itemHeight: 16, makeItem, bindItem);
        listView.selectionType = SelectionType.Multiple;

        // Ajoutez la ListView � l'�l�ment visuel racine
        rootVisualElement.Add(listView);
    }
}


//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UIElements;
//using System.Collections.Generic;
//using System;

//public class TestListView : EditorWindow
//{
//    [MenuItem("MaestroTest/ListViewExample")]
//    public static void ShowExample()
//    {
//        TestListView wnd = GetWindow<TestListView>();
//        wnd.titleContent = new GUIContent("ListViewExample");
//    }

//    public void OnEnable()
//    {
//        // Create some list of data, here simply numbers in interval [1, 1000]
//        const int itemCount = 1000;
//        var items = new List<string>(itemCount);
//        for (int i = 1; i <= itemCount; i++)
//            items.Add(i.ToString());

//        // Define makeItem and bindItem functions
//        Func<VisualElement> makeItem = () => new Label();
//        Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = items[i];

//        // Create a new ListView
//        ListView listView = new ListView(items, itemHeight: 16, makeItem, bindItem)
//        {
//            selectionType = SelectionType.Multiple
//        };

//        // Add callbacks for item chosen and selection changed events
//        listView.onItemsChosen += chosenItems => Debug.Log($"User double-clicked on: {string.Join(", ", chosenItems)}");
//        listView.onSelectionChange += selectedItems => Debug.Log($"User selected: {string.Join(", ", selectedItems)}");

//        // Add the ListView to the root visual element
//        rootVisualElement.Add(listView);
//    }
//}
#endif