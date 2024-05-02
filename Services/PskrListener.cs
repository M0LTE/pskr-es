using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace pskr_es.Services;

internal class PskrListener : IHostedService
{
    private IManagedMqttClient? _mqttClient;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient = new MqttFactory().CreateManagedMqttClient();

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer("mqtt.pskreporter.info")
                .Build())
        .Build();

        const int england = 223;
        const int scotland = 279;
        const int wales = 294;
        const int ni = 265;
        const int ei = 245;
        const int iom = 114;

        List<MqttTopicFilter> topicFilters = [];

        var homeNations = new[] { england, scotland, wales, ni, ei, iom };

        foreach (int entity in homeNations)
        {
            topicFilters.Add(new MqttTopicFilterBuilder().WithTopic($"pskr/filter/v2/6m/FT8/+/+/+/+/{entity}/+").Build());
            topicFilters.Add(new MqttTopicFilterBuilder().WithTopic($"pskr/filter/v2/6m/FT8/+/+/+/+/+/{entity}").Build());
        }

        await _mqttClient.SubscribeAsync(topicFilters);
        await _mqttClient.StartAsync(options);

        _mqttClient.ApplicationMessageReceivedAsync += arg =>
        {
            var obj = JsonNode.Parse(arg.ApplicationMessage.ConvertPayloadToString());
            var senderEntity = obj!["sa"]?.GetValue<int>();
            var receiverEntity = obj!["ra"]?.GetValue<int>();

            if (senderEntity != null && receiverEntity != null && senderEntity != receiverEntity)
            {
                if (!homeNations.Any(i => i == senderEntity) || !homeNations.Any(i => i == receiverEntity))
                {
                    if (cty.TryGetValue(senderEntity.Value, out var senderCountry)
                         && cty.TryGetValue(receiverEntity.Value, out var receiverCountry))
                    {
                        var recent = pairs.Where(a => (a.Item2 == senderEntity && a.Item3 == receiverEntity)
                            || (a.Item2 == receiverEntity && a.Item3 == senderEntity));

                        if (!recent.Any() || recent.All(r => r.Item1.Elapsed > TimeSpan.FromMinutes(15)))
                        {
                            var ts = new DateTime(1970, 1, 1).AddSeconds(obj!["t"]!.GetValue<long>());
                            Console.WriteLine($"{ts:HH:mm:ss}Z {receiverCountry} hearing {senderCountry}");
                            pairs.RemoveAll(a => (a.Item2 == senderEntity && a.Item3 == receiverEntity)
                                || (a.Item2 == receiverEntity && a.Item3 == senderEntity));
                            pairs.Add(Tuple.Create(Stopwatch.StartNew(), senderEntity.Value, receiverEntity.Value));
                        }
                    }
                }
            }

            return Task.CompletedTask;
        };
    }

    private readonly List<Tuple<Stopwatch, int, int>> pairs = [];

    private readonly Dictionary<int, string> cty = new()
    {
        { 246, "Sov Mil Order of Malta" },
        { 247, "Spratly Islands" },
        { 260, "Monaco" },
        { 4, "Agalega & St. Brandon" },
        { 165, "Mauritius" },
        { 207, "Rodriguez Island" },
        { 49, "Equatorial Guinea" },
        { 195, "Annobon Island" },
        { 176, "Fiji" },
        { 489, "Conway Reef" },
        { 460, "Rotuma Island" },
        { 468, "Kingdom of Eswatini" },
        { 474, "Tunisia" },
        { 293, "Vietnam" },
        { 107, "Guinea" },
        { 24, "Bouvet" },
        { 199, "Peter 1 Island" },
        { 18, "Azerbaijan" },
        { 75, "Georgia" },
        { 514, "Montenegro" },
        { 315, "Sri Lanka" },
        { 117, "ITU HQ" },
        { 289, "United Nations HQ" },
        { 511, "Timor - Leste" },
        { 336, "Israel" },
        { 436, "Libya" },
        { 215, "Cyprus" },
        { 470, "Tanzania" },
        { 450, "Nigeria" },
        { 438, "Madagascar" },
        { 444, "Mauritania" },
        { 187, "Niger" },
        { 483, "Togo" },
        { 190, "Samoa" },
        { 286, "Uganda" },
        { 430, "Kenya" },
        { 456, "Senegal" },
        { 82, "Jamaica" },
        { 492, "Yemen" },
        { 432, "Lesotho" },
        { 440, "Malawi" },
        { 400, "Algeria" },
        { 62, "Barbados" },
        { 159, "Maldives" },
        { 129, "Guyana" },
        { 497, "Croatia" },
        { 424, "Ghana" },
        { 257, "Malta" },
        { 482, "Zambia" },
        { 348, "Kuwait" },
        { 458, "Sierra Leone" },
        { 299, "West Malaysia" },
        { 46, "East Malaysia" },
        { 369, "Nepal" },
        { 414, "Dem. Rep. of the Congo" },
        { 404, "Burundi" },
        { 381, "Singapore" },
        { 454, "Rwanda" },
        { 90, "Trinidad & Tobago" },
        { 402, "Botswana" },
        { 160, "Tonga" },
        { 370, "Oman" },
        { 306, "Bhutan" },
        { 391, "United Arab Emirates" },
        { 376, "Qatar" },
        { 304, "Bahrain" },
        { 372, "Pakistan" },
        { 506, "Scarborough Reef" },
        { 386, "Taiwan" },
        { 505, "Pratas Island" },
        { 318, "China" },
        { 157, "Nauru" },
        { 203, "Andorra" },
        { 422, "The Gambia" },
        { 60, "Bahamas" },
        { 181, "Mozambique" },
        { 112, "Chile" },
        { 217, "San Felix & San Ambrosio" },
        { 47, "Easter Island" },
        { 125, "Juan Fernandez Islands" },
        { 13, "Antarctica" },
        { 70, "Cuba" },
        { 446, "Morocco" },
        { 104, "Bolivia" },
        { 272, "Portugal" },
        { 256, "Madeira Islands" },
        { 149, "Azores" },
        { 144, "Uruguay" },
        { 211, "Sable Island" },
        { 252, "St. Paul Island" },
        { 401, "Angola" },
        { 409, "Cape Verde" },
        { 411, "Comoros" },
        { 230, "Germany" },
        { 375, "Philippines" },
        { 51, "Eritrea" },
        { 510, "Palestine" },
        { 191, "North Cook Islands" },
        { 234, "South Cook Islands" },
        { 188, "Niue" },
        { 501, "Bosnia-Herzegovina" },
        { 281, "Spain" },
        { 21, "Balearic Islands" },
        { 29, "Canary Islands" },
        { 32, "Ceuta & Melilla" },
        { 245, "Ireland" },
        { 14, "Armenia" },
        { 434, "Liberia" },
        { 330, "Iran" },
        { 179, "Moldova" },
        { 52, "Estonia" },
        { 53, "Ethiopia" },
        { 27, "Belarus" },
        { 135, "Kyrgyzstan" },
        { 262, "Tajikistan" },
        { 280, "Turkmenistan" },
        { 227, "France" },
        { 79, "Guadeloupe" },
        { 169, "Mayotte" },
        { 516, "St. Barthelemy" },
        { 162, "New Caledonia" },
        { 512, "Chesterfield Islands" },
        { 84, "Martinique" },
        { 175, "French Polynesia" },
        { 508, "Austral Islands" },
        { 36, "Clipperton Island" },
        { 509, "Marquesas Islands" },
        { 277, "St. Pierre & Miquelon" },
        { 453, "Reunion Island" },
        { 213, "St. Martin" },
        { 99, "Glorioso Islands" },
        { 124, "Juan de Nova & Europa" },
        { 276, "Tromelin Island" },
        { 41, "Crozet Island" },
        { 131, "Kerguelen Islands" },
        { 10, "Amsterdam & St. Paul Is." },
        { 298, "Wallis & Futuna Islands" },
        { 63, "French Guiana" },
        { 223, "England" },
        { 114, "Isle of Man" },
        { 265, "Northern Ireland" },
        { 122, "Jersey" },
        { 279, "Scotland" },
        { 106, "Guernsey" },
        { 294, "Wales" },
        { 185, "Solomon Islands" },
        { 507, "Temotu Province" },
        { 239, "Hungary" },
        { 287, "Switzerland" },
        { 251, "Liechtenstein" },
        { 120, "Ecuador" },
        { 71, "Galapagos Islands" },
        { 78, "Haiti" },
        { 72, "Dominican Republic" },
        { 116, "Colombia" },
        { 216, "San Andres & Providencia" },
        { 161, "Malpelo Island" },
        { 137, "Republic of Korea" },
        { 88, "Panama" },
        { 80, "Honduras" },
        { 387, "Thailand" },
        { 295, "Vatican City" },
        { 378, "Saudi Arabia" },
        { 248, "Italy" },
        { 225, "Sardinia" },
        { 382, "Djibouti" },
        { 77, "Grenada" },
        { 109, "Guinea-Bissau" },
        { 97, "St. Lucia" },
        { 95, "Dominica" },
        { 98, "St. Vincent" },
        { 339, "Japan" },
        { 177, "Minami Torishima" },
        { 192, "Ogasawara" },
        { 363, "Mongolia" },
        { 259, "Svalbard/Bear Island" },
        { 118, "Jan Mayen" },
        { 342, "Jordan" },
        { 291, "United States" },
        { 105, "Guantanamo Bay" },
        { 166, "Mariana Islands" },
        { 20, "Baker & Howland Islands" },
        { 103, "Guam" },
        { 123, "Johnston Island" },
        { 174, "Midway Island" },
        { 197, "Palmyra & Jarvis Islands" },
        { 110, "Hawaii" },
        { 138, "Kure Island" },
        { 9, "American Samoa" },
        { 515, "Swains Island" },
        { 297, "Wake Island" },
        { 6, "Alaska" },
        { 182, "Navassa Island" },
        { 285, "US Virgin Islands" },
        { 202, "Puerto Rico" },
        { 43, "Desecheo Island" },
        { 266, "Norway" },
        { 100, "Argentina" },
        { 254, "Luxembourg" },
        { 146, "Lithuania" },
        { 212, "Bulgaria" },
        { 136, "Peru" },
        { 354, "Lebanon" },
        { 206, "Austria" },
        { 224, "Finland" },
        { 5, "Aland Islands" },
        { 167, "Market Reef" },
        { 503, "Czech Republic" },
        { 504, "Slovak Republic" },
        { 209, "Belgium" },
        { 237, "Greenland" },
        { 222, "Faroe Islands" },
        { 221, "Denmark" },
        { 163, "Papua New Guinea" },
        { 91, "Aruba" },
        { 344, "DPR of Korea" },
        { 263, "Netherlands" },
        { 517, "Curacao" },
        { 520, "Bonaire" },
        { 519, "Saba & St. Eustatius" },
        { 518, "Sint Maarten" },
        { 108, "Brazil" },
        { 56, "Fernando de Noronha" },
        { 253, "St. Peter & St. Paul" },
        { 273, "Trindade & Martim Vaz" },
        { 140, "Suriname" },
        { 61, "Franz Josef Land" },
        { 302, "Western Sahara" },
        { 305, "Bangladesh" },
        { 499, "Slovenia" },
        { 379, "Seychelles" },
        { 219, "Sao Tome & Principe" },
        { 284, "Sweden" },
        { 269, "Poland" },
        { 466, "Sudan" },
        { 478, "Egypt" },
        { 236, "Greece" },
        { 180, "Mount Athos" },
        { 45, "Dodecanese" },
        { 40, "Crete" },
        { 282, "Tuvalu" },
        { 301, "Western Kiribati" },
        { 31, "Central Kiribati" },
        { 48, "Eastern Kiribati" },
        { 490, "Banaba Island" },
        { 232, "Somalia" },
        { 278, "San Marino" },
        { 22, "Palau" },
        { 390, "Turkey" },
        { 242, "Iceland" },
        { 76, "Guatemala" },
        { 308, "Costa Rica" },
        { 37, "Cocos Island" },
        { 406, "Cameroon" },
        { 214, "Corsica" },
        { 408, "Central African Republic" },
        { 412, "Republic of the Congo" },
        { 420, "Gabon" },
        { 410, "Chad" },
        { 428, "Cote d'Ivoire" },
        { 416, "Benin" },
        { 442, "Mali" },
        { 54, "European Russia" },
        { 126, "Kaliningrad" },
        { 15, "Asiatic Russia" },
        { 292, "Uzbekistan" },
        { 130, "Kazakhstan" },
        { 288, "Ukraine" },
        { 94, "Antigua & Barbuda" },
        { 66, "Belize" },
        { 249, "St. Kitts & Nevis" },
        { 464, "Namibia" },
        { 173, "Micronesia" },
        { 168, "Marshall Islands" },
        { 345, "Brunei Darussalam" },
        { 1, "Canada" },
        { 150, "Australia" },
        { 111, "Heard Island" },
        { 153, "Macquarie Island" },
        { 38, "Cocos (Keeling) Islands" },
        { 147, "Lord Howe Island" },
        { 171, "Mellish Reef" },
        { 189, "Norfolk Island" },
        { 303, "Willis Island" },
        { 35, "Christmas Island" },
        { 12, "Anguilla" },
        { 96, "Montserrat" },
        { 65, "British Virgin Islands" },
        { 89, "Turks & Caicos Islands" },
        { 172, "Pitcairn Island" },
        { 513, "Ducie Island" },
        { 141, "Falkland Islands" },
        { 235, "South Georgia Island" },
        { 241, "South Shetland Islands" },
        { 238, "South Orkney Islands" },
        { 240, "South Sandwich Islands" },
        { 64, "Bermuda" },
        { 33, "Chagos Islands" },
        { 321, "Hong Kong" },
        { 324, "India" },
        { 11, "Andaman & Nicobar Is." },
        { 142, "Lakshadweep Islands" },
        { 50, "Mexico" },
        { 204, "Revillagigedo" },
        { 480, "Burkina Faso" },
        { 312, "Cambodia" },
        { 143, "Laos" },
        { 152, "Macao" },
        { 309, "Myanmar" },
        { 3, "Afghanistan" },
        { 327, "Indonesia" },
        { 333, "Iraq" },
        { 158, "Vanuatu" },
        { 384, "Syria" },
        { 145, "Latvia" },
        { 86, "Nicaragua" },
        { 275, "Romania" },
        { 74, "El Salvador" },
        { 296, "Serbia" },
        { 148, "Venezuela" },
        { 17, "Aves Island" },
        { 452, "Zimbabwe" },
        { 502, "North Macedonia" },
        { 522, "Republic of Kosovo" },
        { 521, "Republic of South Sudan" },
        { 7, "Albania" },
        { 233, "Gibraltar" },
        { 283, "UK Base Areas on Cyprus" },
        { 250, "St. Helena" },
        { 205, "Ascension Island" },
        { 274, "Tristan da Cunha & Gough Islands" },
        { 69, "Cayman Islands" },
        { 270, "Tokelau Islands" },
        { 170, "New Zealand" },
        { 34, "Chatham Islands" },
        { 133, "Kermadec Islands" },
        { 16, "N.Z. Subantarctic Is." },
        { 132, "Paraguay" },
        { 462, "South Africa" },
        { 201, "Pr. Edward & Marion Is." },
    };

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

