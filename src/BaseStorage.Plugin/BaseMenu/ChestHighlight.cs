namespace BaseStorage.Plugin.BaseMenu;

using System;
using BaseStorage.Core.Abstractions;
using UnityEngine;

/// <summary>
/// Makes one chest glow so it can be found in the base, with the same effect the hammer's remove mode uses. That effect fades by
/// itself almost at once, so it is renewed until the time runs out. Runs with the inventory closed. Main thread only.
/// </summary>
internal static class ChestHighlight
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Renew = TimeSpan.FromSeconds(0.1);

    private const string ShareRpc = "BaseStorage_MarkChest";
    private static ZRoutedRpc? registeredOn;
    private static IClock? clock;
    private static WearNTear? target;
    private static Container? chest;
    private static TimeSpan until;
    private static TimeSpan nextRenew;

    /// <summary>Stores the clock.</summary>
    /// <param name="time">The clock.</param>
    internal static void Initialize(IClock time) => clock = time;

    /// <summary>Starts the glow on a chest, or stops it if that chest is already glowing.</summary>
    /// <param name="container">The chest.</param>
    internal static void Toggle(Container container)
    {
        bool same = ReferenceEquals(chest, container);
        Stop();
        if (!same)
        {
            Show(container);
        }
    }

    /// <summary>Pings the chest for everyone on the map and asks every player running the mod to glow it. The sender's own copy arrives too.</summary>
    /// <param name="container">The chest.</param>
    internal static void Share(Container container)
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        ZNetView view = container.m_nview;
        if (rpc != null && view != null && view.IsValid())
        {
            rpc.InvokeRoutedRPC(ZRoutedRpc.Everybody, ShareRpc, view.GetZDO().m_uid);
        }

        if (Chat.instance != null)
        {
            Chat.instance.SendPing(container.transform.position);
        }
    }

    private static void Show(Container container)
    {
        if (clock == null || container == null)
        {
            return;
        }

        target = container.GetComponent<WearNTear>();
        if (target != null)
        {
            chest = container;
            until = clock.Elapsed + Duration;
            nextRenew = TimeSpan.Zero;
        }
    }

    /// <summary>Registers the share message once per network session, which is a new ZRoutedRpc each time.</summary>
    private static void Register()
    {
        ZRoutedRpc rpc = ZRoutedRpc.instance;
        if (rpc != null && !ReferenceEquals(rpc, registeredOn))
        {
            registeredOn = rpc;
            rpc.Register<ZDOID>(ShareRpc, OnShared);
        }
    }

    /// <summary>A chest this client has not loaded is too far to see, so it is skipped; the map ping still shows the place.</summary>
    private static void OnShared(long sender, ZDOID id)
    {
        ZNetScene scene = ZNetScene.instance;
        GameObject? found = scene == null ? null : scene.FindInstance(id);
        Container? container = found == null ? null : found.GetComponent<Container>();
        if (container != null)
        {
            Show(container);
        }
    }

    /// <summary>Renews the glow. Called once per frame.</summary>
    internal static void Tick()
    {
        Register();
        if (chest == null || clock == null)
        {
            return;
        }

        if (target == null || clock.Elapsed >= until)
        {
            Stop();
            return;
        }

        if (clock.Elapsed >= nextRenew)
        {
            nextRenew = clock.Elapsed + Renew;
            target.Highlight();
        }
    }

    private static void Stop()
    {
        chest = null;
        target = null;
    }
}
