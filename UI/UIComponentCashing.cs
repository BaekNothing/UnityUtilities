public static class UIObj
{
    public static T GetT<T>(Transform parent, string targetName) where T : Component
        => parent.Find(targetName.Substring(1, targetName.Length - 1))?.GetComponent<T>();

    public static T GetEldistT<T>(Transform parent) where T : Component
    {
        T target;
        for (int i = 0; i < parent.childCount; i++)
        {
            target = parent.GetChild(i).GetComponent<T>();
            if (target != null)
                return target;
        }
        return null;
    }
    
    public static void SetAction(Button btn, UnityEngine.Events.UnityAction action)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }
}
