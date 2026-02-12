using UnityEngine;
using UnityEngine.SceneManagement;

public static class RuntimeSpawnGroups
{
    private const string RootName = "[Runtime]";
    private const string PlayerBulletsName = "Player Bullets";
    private const string EnemyBulletsName = "Enemy Bullets";

    private static int cachedSceneHandle = int.MinValue;
    private static Transform root;
    private static Transform playerBullets;
    private static Transform enemyBullets;

    public static Transform GetPlayerBulletsGroup()
    {
        return GetOrCreateGroup(ref playerBullets, PlayerBulletsName);
    }

    public static Transform GetEnemyBulletsGroup()
    {
        return GetOrCreateGroup(ref enemyBullets, EnemyBulletsName);
    }

    public static void MoveToPlayerBullets(Transform target)
    {
        MoveToGroup(target, GetPlayerBulletsGroup());
    }

    public static void MoveToEnemyBullets(Transform target)
    {
        MoveToGroup(target, GetEnemyBulletsGroup());
    }

    private static Transform GetOrCreateGroup(ref Transform cachedGroup, string groupName)
    {
        if (!Application.isPlaying)
        {
            return null;
        }

        RefreshCacheIfSceneChanged();
        if (cachedGroup != null)
        {
            return cachedGroup;
        }

        Transform runtimeRoot = GetOrCreateRoot();
        Transform existing = runtimeRoot.Find(groupName);
        if (existing != null)
        {
            cachedGroup = existing;
            return cachedGroup;
        }

        GameObject groupObject = new GameObject(groupName);
        cachedGroup = groupObject.transform;
        cachedGroup.SetParent(runtimeRoot, false);
        return cachedGroup;
    }

    private static Transform GetOrCreateRoot()
    {
        if (root != null)
        {
            return root;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == RootName)
            {
                root = roots[i].transform;
                return root;
            }
        }

        GameObject rootObject = new GameObject(RootName);
        root = rootObject.transform;
        SceneManager.MoveGameObjectToScene(rootObject, activeScene);
        return root;
    }

    private static void MoveToGroup(Transform target, Transform group)
    {
        if (target == null || group == null || target.parent == group)
        {
            return;
        }

        target.SetParent(group, true);
    }

    private static void RefreshCacheIfSceneChanged()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.handle == cachedSceneHandle)
        {
            return;
        }

        cachedSceneHandle = activeScene.handle;
        root = null;
        playerBullets = null;
        enemyBullets = null;
    }
}
