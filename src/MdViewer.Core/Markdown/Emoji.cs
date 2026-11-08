namespace MdViewer.Core.Markdown;

/// <summary>
/// GitHub emoji shortcodes. The table covers the aliases that actually appear in
/// technical writing; unknown <c>:codes:</c> and clock strings pass through
/// untouched, exactly like MD.Scribe does for unknown names.
/// </summary>
public static class Emoji
{
    private const string Table =
        "grinning=😀;smiley=😃;smile=😄;grin=😁;laughing=😆;sweat_smile=😅;joy=😂;rofl=🤣;" +
        "relaxed=☺️;blush=😊;innocent=😇;wink=😉;yum=😋;relieved=😌;heart_eyes=😍;kissing_heart=😘;" +
        "thinking=🤔;neutral_face=😐;expressionless=😑;no_mouth=😶;smirk=😏;unamused=😒;roll_eyes=🙄;" +
        "grimacing=😬;pensive=😔;sleepy=😪;mask=😷;nerd_face=🤓;sunglasses=😎;confused=😕;worried=😟;" +
        "cry=😢;sob=😭;angry=😠;rage=😡;triumph=😤;fearful=😨;cold_sweat=😰;scream=😱;" +
        "thumbsup=👍;+1=👍;thumbsdown=👎;-1=👎;ok_hand=👌;clap=👏;wave=👋;pray=🙏;muscle=💪;" +
        "point_right=👉;point_left=👈;point_up=☝️;point_down=👇;raised_hand=✋;hand=✋;" +
        "eyes=👀;brain=🧠;heart=❤️;broken_heart=💔;sparkling_heart=💖;fire=🔥;star=⭐;star2=🌟;" +
        "zap=⚡;boom=💥;collision=💥;sparkles=✨;sunny=☀️;cloud=☁️;zap2=⚡;snowflake=❄️;" +
        "droplet=💧;ocean=🌊;rocket=🚀;airplane=✈️;car=🚗;bike=🚲;train=🚆;ship=🚢;" +
        "computer=💻;desktop=🖥️;keyboard=⌨️;printer=🖨️;floppy_disk=💾;cd=💿;dvd=📀;" +
        "phone=☎️;iphone=📱;calling=📲;battery=🔋;electric_plug=🔌;bulb=💡;flashlight=🔦;" +
        "wrench=🔧;hammer=🔨;nut_and_bolt=🔩;gear=⚙️;toolbox=🧰;magnet=🧲;link=🔗;paperclip=📎;" +
        "lock=🔒;unlock=🔓;key=🔑;shield=🛡️;closed_lock_with_key=🔐;old_key=🗝️;" +
        "book=📖;books=📚;notebook=📓;ledger=📒;page_facing_up=📄;page_with_curl=📃;scroll=📜;" +
        "memo=📝;pencil2=✏️;pen=🖊️;pushpin=📌;round_pushpin=📍;clipboard=📋;file_folder=📁;" +
        "open_file_folder=📂;card_index_dividers=🗂️;calendar=📆;date=📅;chart_with_upwards_trend=📈;" +
        "chart_with_downwards_trend=📉;bar_chart=📊;mag=🔍;mag_right=🔎;microscope=🔬;telescope=🔭;" +
        "package=📦;inbox_tray=📥;outbox_tray=📤;email=📧;envelope=✉️;mailbox=📫;bell=🔔;" +
        "no_bell=🔕;loudspeaker=📢;mega=📣;speech_balloon=💬;thought_balloon=💭;" +
        "warning=⚠️;no_entry=⛔;no_entry_sign=🚫;x=❌;heavy_check_mark=✔️;white_check_mark=✅;" +
        "ballot_box_with_check=☑️;heavy_exclamation_mark=❗;question=❓;exclamation=❗;grey_question=❔;" +
        "information_source=ℹ️;arrow_right=➡️;arrow_left=⬅️;arrow_up=⬆️;arrow_down=⬇️;" +
        "arrow_forward=▶️;arrow_backward=◀️;arrows_counterclockwise=🔄;repeat=🔁;twisted_rightwards_arrows=🔀;" +
        "hourglass=⌛;hourglass_flowing_sand=⏳;alarm_clock=⏰;stopwatch=⏱️;timer_clock=⏲️;" +
        "watch=⌚;clock1=🕐;calendar_spiral=🗓️;construction=🚧;hammer_and_wrench=🛠️;" +
        "white_flower=💮;cherry_blossom=🌸;four_leaf_clover=🍀;maple_leaf=🍁;herb=🌿;" +
        "deciduous_tree=🌳;evergreen_tree=🌲;cactus=🌵;mushroom=🍄;rose=🌹;sunflower=🌻;" +
        "earth_africa=🌍;earth_americas=🌎;earth_asia=🌏;globe_with_meridians=🌐;moon=🌔;" +
        "coffee=☕;tea=🍵;beer=🍺;beers=🍻;wine_glass=🍷;cocktail=🍸;tropical_drink=🍹;" +
        "pizza=🍕;hamburger=🍔;fries=🍟;apple=🍎;egg=🥚;bread=🍞;cake=🍰;birthday=🎂;" +
        "tada=🎉;confetti_ball=🎊;gift=🎁;trophy=🏆;medal_sports=🏅;1st_place_medal=🥇;" +
        "soccer=⚽;basketball=🏀;football=🏈;video_game=🎮;dart=🎯;game_die=🎲;art=🎨;" +
        "musical_note=🎵;notes=🎶;headphones=🎧;microphone=🎤;movie_camera=🎥;camera=📷;" +
        "cityscape=🏙️;house=🏠;office=🏢;factory=🏭;construction_site=🏗️;bridge_at_night=🌉;" +
        "dog=🐶;cat=🐱;mouse=🐭;fish=🐟;bug=🐛;bee=🐝;butterfly=🦋;snake=🐍;" +
        "penguin=🐧;whale=🐳;dolphin=🐬;octopus=🐙;crab=🦀;turtle=🐢;" +
        "wave2=👋;clinking_glasses=🥂;label=🏷️;triangular_flag_on_post=🚩;checkered_flag=🏁;" +
        "package2=📦;sparkle=❇️;diamonds=♦️;clubs2=♣️;spades2=♠️;hearts2=♥️;";

    private static readonly Dictionary<string, string> Map = BuildMap();

    private static Dictionary<string, string> BuildMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in Table.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = entry.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            map[entry[..equals]] = entry[(equals + 1)..];
        }

        return map;
    }

    public static int Count => Map.Count;

    /// <summary>Resolves <paramref name="name"/> (without colons); false for unknown codes.</summary>
    public static bool TryGet(string name, out string value) => Map.TryGetValue(name, out value!);

    /// <summary>
    /// Scans <c>:name:</c> at <paramref name="index"/>; on success returns the
    /// resolved character and moves <paramref name="index"/> past the code.
    /// </summary>
    public static bool TryParseAt(string text, ref int index, out string value)
    {
        value = string.Empty;
        if (index >= text.Length || text[index] != ':')
        {
            return false;
        }

        int close = text.IndexOf(':', index + 1);
        if (close < 0 || close == index + 1 || close - index > 40)
        {
            return false;
        }

        string name = text[(index + 1)..close];
        foreach (char c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-' or '+'))
            {
                return false;
            }
        }

        if (!Map.TryGetValue(name, out value!))
        {
            return false;
        }

        index = close + 1;
        return true;
    }
}
