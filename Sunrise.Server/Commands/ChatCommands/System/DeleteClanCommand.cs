using Sunrise.Server.Attributes;
using Sunrise.Server.Repositories;
using Sunrise.Shared.Application;
using Sunrise.Shared.Database;
using Sunrise.Shared.Database.Repositories;
using Sunrise.Shared.Enums.Users;
using Sunrise.Shared.Objects;
using Sunrise.Shared.Objects.Sessions;
using Sunrise.Shared.Services;

namespace Sunrise.Server.Commands.ChatCommands.System;

[ChatCommand("deleteclan", requiredPrivileges: UserPrivilege.Admin)]
public class DeleteClanCommand : IChatCommand
{
    public Task Handle(Session session, ChatChannel? channel, string[]? args)
    {
        if (args == null || args.Length < 1)
        {
            ChatCommandRepository.SendMessage(session,
                $"Usage: {Configuration.BotPrefix}deleteclan <clanId>; Example: {Configuration.BotPrefix}deleteclan 1");
            return Task.CompletedTask;
        }

        if (!int.TryParse(args[0], out var clanId) || clanId < 1)
        {
            ChatCommandRepository.SendMessage(session, "Invalid clan id.");
            return Task.CompletedTask;
        }

        BackgroundTaskService.TryStartNewBackgroundJob<DeleteClanCommand>(
            () => DeleteClan(session.UserId, clanId),
            message => ChatCommandRepository.SendMessage(session, message));

        return Task.CompletedTask;
    }

    public async Task DeleteClan(int userId, int clanId)
    {
        await BackgroundTaskService.ExecuteBackgroundTask<DeleteClanCommand>(
            async () =>
            {
                using var scope = ServicesProviderHolder.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();

                var deleteResult = await database.Clans.DeleteClanByAdmin(clanId);

                if (deleteResult != ClanRepository.DeleteClanByAdminResult.Success)
                {
                    if (deleteResult == ClanRepository.DeleteClanByAdminResult.ClanNotFound)
                    {
                        ChatCommandRepository.TrySendMessage(userId, $"Clan {clanId} not found.");
                        return;
                    }

                    ChatCommandRepository.TrySendMessage(userId,
                        $"Failed to delete clan {clanId}. Unexpected result: {deleteResult}");
                    return;
                }

                ChatCommandRepository.TrySendMessage(userId, $"Clan {clanId} has been deleted.");
            },
            message => ChatCommandRepository.TrySendMessage(userId, message));
    }
}
