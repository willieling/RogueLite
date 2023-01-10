using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct ProjectileData
{
    [SerializeField]
    private float _speed;

    // The amount of time between shots
    [SerializeField]
    private float _cooldown;

    public float Speed { get => _speed; set => _speed = value; }
    public float Cooldown { get => _cooldown; set => _cooldown = value; }
}

public class ProjectileScriptableObject : ScriptableObject
{
    [MenuItem("Assets/Create/Data Assets/Projectile Scriptable Object")]
    public static void CreateMyAsset()
    {
        ProjectileScriptableObject asset = ScriptableObject.CreateInstance<ProjectileScriptableObject>();

        AssetDatabase.CreateAsset(asset, "Assets/Data/Projectiles/NewScripableObject.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }

    [SerializeField]
    private ProjectileData _data;

    [SerializeField]
    private ProjectileBullet _prefab;

    public ProjectileData Data { get => _data; }
    public ProjectileBullet Prefab { get => _prefab; }
}
