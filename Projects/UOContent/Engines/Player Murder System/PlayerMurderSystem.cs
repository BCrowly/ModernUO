using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Server.Collections;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.PlayerMurderSystem;

public static class PlayerMurderSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PlayerMurderSystem));

    // All of the players with murders
    private static readonly Dictionary<PlayerMobile, MurderContext> _murderContexts = new();

    // Only the players that are online
    private static readonly HashSet<MurderContext> _contextTerms = new(MurderContext.EqualityComparer.Default);

    private static readonly Timer _murdererTimer = new MurdererTimer();

    private static TimeSpan _shortTermMurderDuration;

    private static TimeSpan _longTermMurderDuration;

    public static TimeSpan ShortTermMurderDuration => _shortTermMurderDuration;

    public static TimeSpan LongTermMurderDuration => _longTermMurderDuration;

    public static void Configure()
    {
        GenericPersistence.Register("PlayerMurders", Serialize, Deserialize);

        _shortTermMurderDuration = ServerConfiguration.GetOrUpdateSetting("murderSystem.shortTermMurderDuration", TimeSpan.FromHours(8));
        _longTermMurderDuration = ServerConfiguration.GetOrUpdateSetting("murderSystem.longTermMurderDuration", TimeSpan.FromHours(40));
    }

    public static void Initialize()
    {
        EventSink.Disconnected += OnDisconnected;
        EventSink.Login += OnLogin;
        EventSink.PlayerDeleted += OnPlayerDeleted;

        _murdererTimer.Start();
    }

    private static void OnPlayerDeleted(Mobile m)
    {
        if (m is PlayerMobile pm && _murderContexts.Remove(pm, out var context))
        {
            _contextTerms.Remove(context);
        }
    }

    // Only used for migrations!
    public static void MigrateContext(PlayerMobile player, TimeSpan shortTerm, TimeSpan longTerm)
    {
        if (!World.Loading)
        {
            logger.Error(
                $"Attempted to call MigrateContext outside of world loading.{Environment.NewLine}{{StackTrace}}",
                new StackTrace()
            );
            return;
        }

        var context = player.GetOrCreateMurderContext();

        // We make a big assumption that by the time this is called, the Mobile/PlayerMobile info is deserialized
        if (Mobile.MurderMigrations?.TryGetValue(player, out var shortTermMurders) == true)
        {
            context.ShortTermMurders = shortTermMurders;
        }

        context.ShortTermElapse = shortTerm;
        context.LongTermElapse = longTerm;
        UpdateMurderContext(context);
    }

    private static void OnLogin(Mobile m)
    {
        if (m is not PlayerMobile pm || !GetMurderContext(pm, out var context))
        {
            return;
        }

        if (context.CheckStart())
        {
            _contextTerms.Add(context);
        }
        else
        {
            _murderContexts.Remove(pm);
            _contextTerms.Remove(context);
        }
    }

    private static void OnDisconnected(Mobile m)
    {
        if (m is PlayerMobile pm && _murderContexts.Remove(pm, out var context))
        {
            _contextTerms.Remove(context);
        }
    }

    private static void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; ++i)
        {
            var context = new MurderContext(reader.ReadEntity<PlayerMobile>());
            context.Deserialize(reader);

            _murderContexts.Add(context.Player, context);
        }
    }

    private static void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_murderContexts.Count);
        foreach (var (m, context) in _murderContexts)
        {
            writer.Write(m);
            context.Serialize(writer);
        }
    }

    public static bool GetMurderContext(this PlayerMobile player, out MurderContext context) =>
        _murderContexts.TryGetValue(player, out context);

    public static MurderContext GetOrCreateMurderContext(this PlayerMobile player)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrAddDefault(_murderContexts, player, out var exists);
        if (!exists)
        {
            context = new MurderContext(player);
        }

        return context;
    }

    public static void ManuallySetShortTermMurders(PlayerMobile player, int shortTermMurders)
    {
        var context = player.GetOrCreateMurderContext();
        context.ShortTermMurders = shortTermMurders;
        UpdateMurderContext(context);
    }

    public static void OnPlayerMurder(PlayerMobile player)
    {
        var context = player.GetOrCreateMurderContext();
        context.ShortTermMurders++;
        player.Kills++;

        context.ResetKillTime();
        UpdateMurderContext(context);
    }

    private static void UpdateMurderContext(MurderContext context)
    {
        var player = context.Player;

        if (!context.CheckStart())
        {
            _murderContexts.Remove(player);
            _contextTerms.Remove(context);
        }
        else if (player.NetState != null)
        {
            _contextTerms.Add(context);
        }
    }

    private class MurdererTimer : Timer
    {
        public MurdererTimer() : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0))
        {
        }

        protected override void OnTick()
        {
            if (_contextTerms.Count == 0)
            {
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();

            foreach (var context in _contextTerms)
            {
                context.DecayKills();
                if (!context.CheckStart())
                {
                    queue.Enqueue(context.Player);
                }
            }

            while (queue.Count > 0)
            {
                if (_murderContexts.Remove((PlayerMobile)queue.Dequeue(), out var context))
                {
                    _contextTerms.Remove(context);
                }
            }
        }
    }
}
