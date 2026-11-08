using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// 把 Data/Weapon 下的 WeaponData 标记为 Addressables，并把远端目录指向 GitHub。
/// 菜单：Virus Doctor/Addressables/*
/// </summary>
public static class WeaponAddressablesSetup
{
    private const string GroupName = "Weapon Data";
    private const string RemoteRoot =
        "https://raw.githubusercontent.com/Ffly-Fish/VirusDoctor/master/ServerData/[BuildTarget]";

    private static readonly string[] DataAssets =
    {
        "Assets/Data/Weapon/Rifle.asset",
        "Assets/Data/Weapon/Pistol.asset",
        "Assets/Data/Weapon/Grenade.asset",
    };

    [MenuItem("Virus Doctor/Addressables/Configure Weapon Data")]
    public static void Configure()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            throw new Exception("AddressableAssetSettings not found.");
        }

        string profileId = settings.activeProfileId;
        settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, RemoteRoot);
        settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, "ServerData/[BuildTarget]");

        settings.BuildRemoteCatalog = true;
        settings.DisableCatalogUpdateOnStartup = false;
        settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

        var group = settings.FindGroup(GroupName);
        if (group == null)
        {
            group = settings.CreateGroup(GroupName, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }

        var schema = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
        schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;

        var updateSchema = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();
        updateSchema.StaticContent = false;

        foreach (string path in DataAssets)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                throw new Exception("Missing weapon data asset: " + path);
            }

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = "WeaponData/" + Path.GetFileNameWithoutExtension(path);
            entry.SetLabel("weapon", true);
        }

        EditorUtility.SetDirty(schema);
        EditorUtility.SetDirty(updateSchema);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();
        Debug.Log("[WeaponAddressables] Configured remote root: " + RemoteRoot);
    }

    [MenuItem("Virus Doctor/Addressables/Build Weapon Content")]
    public static void BuildContent()
    {
        Configure();

        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError("[WeaponAddressables] Build failed: " + result.Error);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[WeaponAddressables] Build finished.");
    }
}
