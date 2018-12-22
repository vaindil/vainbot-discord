﻿using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VainBot.Classes.Reminders;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class ReminderService
    {
        readonly DiscordSocketClient _discord;

        readonly ILogger<ReminderService> _logger;
        readonly IServiceProvider _provider;

        readonly List<TimerWrapper> _timers = new List<TimerWrapper>();

        public ReminderService(
            DiscordSocketClient discord,
            ILogger<ReminderService> logger,
            IServiceProvider provider)
        {
            _discord = discord;

            _logger = logger;
            _provider = provider;
        }

        public async Task InitializeAsync()
        {
            List<Reminder> reminders;

            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    reminders = await db.Reminders.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Reminder service could not be initialized");
                return;
            }

            var guilds = reminders
                .Where(r => r.GuildId.HasValue)
                .Select(r => _discord.GetGuild((ulong)r.GuildId))
                .Where(r => r != null)
                .Distinct();
            await _discord.DownloadUsersAsync(guilds);

            var now = DateTimeOffset.UtcNow;

            for (var i = _timers.Count - 1; i >= 0; i--)
            {
                var t = _timers[i];
                t.Timer.Dispose();
                _timers.RemoveAt(i);
            }

            foreach (var r in reminders)
            {
                if (r.FireAt <= now)
                    await SendReminderAsync(r);
                else
                    CreateTimer(r);
            }
        }

        public async Task CreateReminderAsync(ulong userId, ulong channelId, ulong? guildId, string message, TimeSpan remindIn)
        {
            message = message.Replace("@everyone", "(@)everyone").Replace("@here", "(@)here");

            var reminder = new Reminder
            {
                CreatedAt = DateTimeOffset.UtcNow,
                FireAt = DateTimeOffset.UtcNow.Add(remindIn),
                UserId = (long)userId,
                ChannelId = (long)channelId,
                GuildId = (long?)guildId,
                Message = message
            };

            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    db.Reminders.Add(reminder);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in reminder service: add reminder");
            }

            CreateTimer(reminder);
        }

        public async Task SendReminderAsync(object reminderIn)
        {
            var reminder = (Reminder)reminderIn;

            var user = _discord.GetUser((ulong)reminder.UserId);
            if (user == null)
                return;

            var message = $"{user.Mention} asked for a reminder: {reminder.Message}";

            if (!reminder.GuildId.HasValue)
            {
                var channel = await user.GetOrCreateDMChannelAsync();
                if (channel == null)
                    return;

                await channel.SendMessageAsync(message);
            }
            else
            {
                if (!(_discord.GetChannel((ulong)reminder.ChannelId) is SocketTextChannel channel))
                    return;

                try
                {
                    await channel.SendMessageAsync(message);
                }
                catch
                {
                    _logger.LogWarning($"No permission to send reminder message to channel {channel.Name}. " +
                        $"Guild: {channel.Guild.Name}");
                }
            }

            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    var thisReminder = await db.Reminders.FindAsync(reminder.Id);
                    if (thisReminder != null)
                    {
                        db.Reminders.Remove(thisReminder);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in reminder service: remove reminder");
                return;
            }

            var wrapper = _timers.Find(t => t.ReminderId == reminder.Id);
            if (wrapper == null)
                return;

            wrapper.Timer.Dispose();
            wrapper.Timer = null;

            _timers.Remove(wrapper);
        }

        private void CreateTimer(object reminderIn)
        {
            var reminder = (Reminder)reminderIn;

            var wrapper = _timers.Find(t => t.ReminderId == reminder.Id);
            if (wrapper != null)
            {
                wrapper.Timer.Dispose();
                wrapper.Timer = null;

                _timers.Remove(wrapper);
            }

            var timeSpan = reminder.FireAt - DateTimeOffset.UtcNow;
            var maxTimeSpan = TimeSpan.FromDays(40);

            if (timeSpan > maxTimeSpan)
            {
                _timers.Add(
                    new TimerWrapper(
                        reminder.Id,
                        new Timer(CreateTimer, reminder, maxTimeSpan, TimeSpan.FromMilliseconds(-1))));
            }
            else
            {
                _timers.Add(
                    new TimerWrapper(
                        reminder.Id,
                        new Timer(async (e) => await SendReminderAsync(e), reminder, timeSpan, TimeSpan.FromMilliseconds(-1))));
            }
        }

        private class TimerWrapper
        {
            public TimerWrapper(int reminderId, Timer timer)
            {
                ReminderId = reminderId;
                Timer = timer;
            }

            public int ReminderId { get; set; }

            public Timer Timer { get; set; }
        }
    }
}
