/*
* No distribution or sales authorized.
* (c) Pixelated_Lagg
*/

#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;
    public static Data data = new Data();
    public ulong GuildID;
    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _commands = new CommandService();
        _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();
        string token = Token.token;
        _client.Log += _client_Log;
        _client.Ready += Ready;
        _client.SlashCommandExecuted += SlashCommandHandler;
        await RegisterCommandsAsync();
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await Task.Delay(-1);
        data.itemCount++;
    }
    private Task _client_Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }
    public async Task RegisterCommandsAsync()
    {
        _client.MessageReceived += HandleCommandAsync;
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }
    private async Task HandleCommandAsync(SocketMessage arg)
    {
        var message = arg as SocketUserMessage;
        var context = new SocketCommandContext(_client, message);
        if (message.Author.IsBot) 
        {
            return;
        }
        int argPos = 0;
        if (message.HasStringPrefix("!", ref argPos))
        {
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
            {
                Console.WriteLine($"Error: {result.ErrorReason} Message: {message}");
            }
        }
    }
    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        GuildID = ((SocketGuildChannel)(command.Channel)).Guild.Id;
        switch (command.Data.Name)
        {
            case "pay":
                if (!_client.GetGuild(GuildID).GetRole(898762815001739374).Members.Contains(_client.GetGuild(GuildID).GetUser(command.User.Id)))
                {
                    command.RespondAsync("You do not have permission to perform this command.");
                    break;
                }
                if (!data.economy.ContainsKey(command.User.Id))
                {
                    Register(command.User.Id);
                }
                data.economy[((SocketGuildUser)(command.Data.Options.ElementAt(0).Value)).Id] += Convert.ToInt32(command.Data.Options.ElementAt(1).Value);
                command.RespondAsync($"Paid `{((SocketGuildUser)(command.Data.Options.ElementAt(0).Value)).Username}` ${Convert.ToInt32(command.Data.Options.ElementAt(1).Value)}.");
                break;
            case "shop":
                data.shop = (from entry in data.shop orderby entry.Value.price descending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Shop";
                StringBuilder sb = new StringBuilder(1000);
                if (data.shop.Count == 0)
                {
                    eb.Description = "No items to display.";
                    break;
                }
                foreach (KeyValuePair<int, Item> kvp in data.shop)
                {
                    sb.Append($"{kvp.Key} - {kvp.Value.name}, ${kvp.Value.price}{Environment.NewLine}");
                }
                eb.Description = sb.ToString();
                command.RespondAsync(embed: eb.Build());
                break;
            case "buy":
                if (!data.shop.ContainsKey(Convert.ToInt32(command.Data.Options.ElementAt(0).Value)))
                {
                    await command.RespondAsync("Item does not exist.");
                    break;
                }
                if (!data.economy.ContainsKey(command.User.Id))
                {
                    Register(command.User.Id);
                    await command.RespondAsync("You cannot afford this item.");
                    break;
                }
                if (data.economy[command.User.Id] < data.shop[Convert.ToInt32(command.Data.Options.ElementAt(0).Value)].price)
                {
                    await command.RespondAsync("You cannot afford this item.");
                    break;
                }
                data.economy[command.User.Id] -= data.shop[Convert.ToInt32(command.Data.Options.ElementAt(0).Value)].price;
                data.itemEconomy[command.User.Id].Add(data.shop[Convert.ToInt32(command.Data.Options.ElementAt(0).Value)]);
                await command.RespondAsync($"Bought `{data.shop[Convert.ToInt32(command.Data.Options.ElementAt(0).Value)].name}` for ${data.shop[Convert.ToInt32(command.Data.Options.ElementAt(0).Value)].price}.");
                break;
            case "add-item":
                if (!_client.GetGuild(GuildID).GetRole(898762815001739374).Members.Contains(_client.GetGuild(GuildID).GetUser(command.User.Id)))
                {
                    command.RespondAsync("You do not have permission to perform this command.");
                    break;
                }
                data.itemCount++;
                data.shop.Add(data.itemCount, new Item((string)command.Data.Options.ElementAt(0).Value, Convert.ToInt32(command.Data.Options.ElementAt(1).Value)));
                command.RespondAsync($"Added `{(string)command.Data.Options.ElementAt(0).Value}` to the shop.");
                break;
            case "refund":
                if (!_client.GetGuild(GuildID).GetRole(898762815001739374).Members.Contains(_client.GetGuild(GuildID).GetUser(command.User.Id)))
                {
                    command.RespondAsync("You do not have permission to perform this command.");
                    break;
                }
                SocketGuildUser user = (SocketGuildUser)(command.Data.Options.ElementAt(0).Value);
                if (!data.itemEconomy.ContainsKey(user.Id))
                {
                    Register(command.User.Id);
                    await command.RespondAsync($"`{user.Username}` did not buy this item.");
                    break;
                }
                if (!data.itemEconomy[user.Id].Contains(data.shop[Convert.ToInt32(command.Data.Options.ElementAt(1).Value)]))
                {
                    await command.RespondAsync($"`{user.Username}` did not buy this item.");
                    break;
                }
                Item item = data.shop[Convert.ToInt32(command.Data.Options.ElementAt(1).Value)];
                data.itemEconomy[user.Id].Remove(item);
                data.economy[user.Id] += item.price;
                await command.RespondAsync($"Refunded `{user.Username}` ${item.price}.");
                break;
            case "register":
                if (!data.economy.ContainsKey(command.User.Id))
                {
                    Register(command.User.Id);
                    await command.RespondAsync("You have been registered.");
                }
                else
                {
                    await command.RespondAsync("You are already registered.");
                }
                break;
        }
    }
    private async Task Register(ulong id)
    {
        data.economy.Add(id, 0);
        data.itemEconomy.Add(id, new List<Item>());
    }
    private async Task Ready()
    {
        //pay <@user or ID> <amount of currency> this command is the one that only the BoD (Board of directors) should have access to, they have to run this command to give users the payment currency.
        var pay = new SlashCommandBuilder();
        pay.WithName("pay");
        pay.WithDescription("Give a user money.");
        pay.AddOption("user", ApplicationCommandOptionType.User, "The user to pay the money to.", true);
        pay.AddOption("money", ApplicationCommandOptionType.Integer, "The money to pay the user.", true);
        _client.CreateGlobalApplicationCommandAsync(pay.Build());

        //shop - displays the shop that shows the items available for purchase that staff can buy. I believe that this command should be able to accessed by everyone that is verified.
        var shop = new SlashCommandBuilder();
        shop.WithName("shop");
        shop.WithDescription("View all of the items for sale in the shop.");
        _client.CreateGlobalApplicationCommandAsync(shop.Build());

        //buy - This one is pretty self explanatory as well, this one should have one argument which is /buy <shopitem#> where number is the number on the list.
        var buy = new SlashCommandBuilder();
        buy.WithName("buy");
        buy.WithDescription("Buy an item from the shop.");
        buy.AddOption("item", ApplicationCommandOptionType.Integer, "The ID of the item to buy.", true);
        _client.CreateGlobalApplicationCommandAsync(buy.Build());

        //Add item - /add-item this command is pretty self explanatory, this command should only be accessible by BoD, the BoD user runs the command and puts the arguments like /add-item <name of item> <price> 
        //and then when the bot should update the shop list with the item. The bot should display the items in order from lowest first to highest last.
        var add = new SlashCommandBuilder();
        add.WithName("add-item");
        add.WithDescription("Add an item to the shop.");
        add.AddOption("name", ApplicationCommandOptionType.String, "The name of the item.", true);
        add.AddOption("money", ApplicationCommandOptionType.Integer, "The price of the item.", true);
        _client.CreateGlobalApplicationCommandAsync(add.Build());

        //Refund - this should be accessible by the BoD only, this is basically the command where if for some reason the item cannot be provided, the BoD member runs the command /refund <user@/userID> <shopitem#> 
        //and then it refunds the user the amount that the staff member paid for said item.
        var refund = new SlashCommandBuilder();
        refund.WithName("refund");
        refund.WithDescription("Refund a user's money for an item.");
        refund.AddOption("user", ApplicationCommandOptionType.User, "The user to refund.", true);
        refund.AddOption("item", ApplicationCommandOptionType.Integer, "The ID of the item to refund.", true);
        _client.CreateGlobalApplicationCommandAsync(refund.Build());

        //register
        var register = new SlashCommandBuilder();
        register.WithName("register");
        register.WithDescription("Register yourself.");
        _client.CreateGlobalApplicationCommandAsync(register.Build());
    }
}