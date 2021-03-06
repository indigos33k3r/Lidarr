using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Parser
{
    public static class IsoCountries
    {
        // see https://wiki.musicbrainz.org/Release_Country
        private static readonly HashSet<IsoCountry> All = new HashSet<IsoCountry>
        {
            new IsoCountry("AF", "Afghanistan" ),
            new IsoCountry("AX", "Åland Islands"),
            new IsoCountry("AL", "Albania"),
            new IsoCountry("DZ", "Algeria"),
            new IsoCountry("AS", "American Samoa"),
            new IsoCountry("AD", "Andorra"),
            new IsoCountry("AO", "Angola"),
            new IsoCountry("AI", "Anguilla"),
            new IsoCountry("AQ", "Antarctica"),
            new IsoCountry("AG", "Antigua and Barbuda"),
            new IsoCountry("AR", "Argentina"),
            new IsoCountry("AM", "Armenia"),
            new IsoCountry("AW", "Aruba"),
            new IsoCountry("AU", "Australia"),
            new IsoCountry("AT", "Austria"),
            new IsoCountry("AZ", "Azerbaijan"),
            new IsoCountry("BS", "Bahamas"),
            new IsoCountry("BH", "Bahrain"),
            new IsoCountry("BD", "Bangladesh"),
            new IsoCountry("BB", "Barbados"),
            new IsoCountry("BY", "Belarus"),
            new IsoCountry("BE", "Belgium"),
            new IsoCountry("BZ", "Belize"),
            new IsoCountry("BJ", "Benin"),
            new IsoCountry("BM", "Bermuda"),
            new IsoCountry("BT", "Bhutan"),
            new IsoCountry("BO", "Bolivia"),
            new IsoCountry("BA", "Bosnia and Herzegovina"),
            new IsoCountry("BW", "Botswana"),
            new IsoCountry("BV", "Bouvet Island"),
            new IsoCountry("BR", "Brazil"),
            new IsoCountry("IO", "British Indian Ocean Territory"),
            new IsoCountry("BN", "Brunei Darussalam"),
            new IsoCountry("BG", "Bulgaria"),
            new IsoCountry("BF", "Burkina Faso"),
            new IsoCountry("BI", "Burundi"),
            new IsoCountry("KH", "Cambodia"),
            new IsoCountry("CM", "Cameroon"),
            new IsoCountry("CA", "Canada"),
            new IsoCountry("CV", "Cape Verde"),
            new IsoCountry("KY", "Cayman Islands"),
            new IsoCountry("CF", "Central African Republic"),
            new IsoCountry("TD", "Chad"),
            new IsoCountry("CL", "Chile"),
            new IsoCountry("CN", "China"),
            new IsoCountry("CX", "Christmas Island"),
            new IsoCountry("CC", "Cocos (Keeling) Islands"),
            new IsoCountry("CO", "Colombia"),
            new IsoCountry("KM", "Comoros"),
            new IsoCountry("CG", "Congo"),
            new IsoCountry("CD", "Congo, The Democratic Republic of the"),
            new IsoCountry("CK", "Cook Islands"),
            new IsoCountry("CR", "Costa Rica"),
            new IsoCountry("CI", "Cote d'Ivoire"),
            new IsoCountry("HR", "Croatia"),
            new IsoCountry("CU", "Cuba"),
            new IsoCountry("CY", "Cyprus"),
            new IsoCountry("XC", "Czechoslovakia"),
            new IsoCountry("CZ", "Czech Republic"),
            new IsoCountry("DK", "Denmark"),
            new IsoCountry("DJ", "Djibouti"),
            new IsoCountry("DM", "Dominica"),
            new IsoCountry("DO", "Dominican Republic"),
            new IsoCountry("XG", "East Germany"),
            new IsoCountry("EC", "Ecuador"),
            new IsoCountry("EG", "Egypt"),
            new IsoCountry("SV", "El Salvador"),
            new IsoCountry("GQ", "Equatorial Guinea"),
            new IsoCountry("ER", "Eritrea"),
            new IsoCountry("EE", "Estonia"),
            new IsoCountry("ET", "Ethiopia"),
            new IsoCountry("XE", "Europe"),
            new IsoCountry("FK", "Falkland Islands (Malvinas)"),
            new IsoCountry("FO", "Faroe Islands"),
            new IsoCountry("FJ", "Fiji"),
            new IsoCountry("FI", "Finland"),
            new IsoCountry("FR", "France"),
            new IsoCountry("GF", "French Guiana"),
            new IsoCountry("PF", "French Polynesia"),
            new IsoCountry("TF", "French Southern Territories"),
            new IsoCountry("GA", "Gabon"),
            new IsoCountry("GM", "Gambia"),
            new IsoCountry("GE", "Georgia"),
            new IsoCountry("DE", "Germany"),
            new IsoCountry("GH", "Ghana"),
            new IsoCountry("GI", "Gibraltar"),
            new IsoCountry("GR", "Greece"),
            new IsoCountry("GL", "Greenland"),
            new IsoCountry("GD", "Grenada"),
            new IsoCountry("GP", "Guadeloupe"),
            new IsoCountry("GU", "Guam"),
            new IsoCountry("GT", "Guatemala"),
            new IsoCountry("GG", "Guernsey"),
            new IsoCountry("GN", "Guinea"),
            new IsoCountry("GW", "Guinea-Bissau"),
            new IsoCountry("GY", "Guyana"),
            new IsoCountry("HT", "Haiti"),
            new IsoCountry("HM", "Heard and Mc Donald Islands"),
            new IsoCountry("HN", "Honduras"),
            new IsoCountry("HK", "Hong Kong"),
            new IsoCountry("HU", "Hungary"),
            new IsoCountry("IS", "Iceland"),
            new IsoCountry("IN", "India"),
            new IsoCountry("ID", "Indonesia"),
            new IsoCountry("IR", "Iran (Islamic Republic of)"),
            new IsoCountry("IQ", "Iraq"),
            new IsoCountry("IE", "Ireland"),
            new IsoCountry("IM", "Isle of Man"),
            new IsoCountry("IL", "Israel"),
            new IsoCountry("IT", "Italy"),
            new IsoCountry("JM", "Jamaica"),
            new IsoCountry("JP", "Japan"),
            new IsoCountry("JE", "Jersey"),
            new IsoCountry("JO", "Jordan"),
            new IsoCountry("KZ", "Kazakhstan"),
            new IsoCountry("KE", "Kenya"),
            new IsoCountry("KI", "Kiribati"),
            new IsoCountry("KP", "Korea (North), Democratic People's Republic of"),
            new IsoCountry("KR", "Korea (South), Republic of"),
            new IsoCountry("KW", "Kuwait"),
            new IsoCountry("KG", "Kyrgyzstan"),
            new IsoCountry("LA", "Lao People's Democratic Republic"),
            new IsoCountry("LV", "Latvia"),
            new IsoCountry("LB", "Lebanon"),
            new IsoCountry("LS", "Lesotho"),
            new IsoCountry("LR", "Liberia"),
            new IsoCountry("LY", "Libyan Arab Jamahiriya"),
            new IsoCountry("LI", "Liechtenstein"),
            new IsoCountry("LT", "Lithuania"),
            new IsoCountry("LU", "Luxembourg"),
            new IsoCountry("MO", "Macau"),
            new IsoCountry("MK", "Macedonia, The Former Yugoslav Republic of"),
            new IsoCountry("MG", "Madagascar"),
            new IsoCountry("MW", "Malawi"),
            new IsoCountry("MY", "Malaysia"),
            new IsoCountry("MV", "Maldives"),
            new IsoCountry("ML", "Mali"),
            new IsoCountry("MT", "Malta"),
            new IsoCountry("MH", "Marshall Islands"),
            new IsoCountry("MQ", "Martinique"),
            new IsoCountry("MR", "Mauritania"),
            new IsoCountry("MU", "Mauritius"),
            new IsoCountry("YT", "Mayotte"),
            new IsoCountry("MX", "Mexico"),
            new IsoCountry("FM", "Micronesia, Federated States of"),
            new IsoCountry("MD", "Moldova, Republic of"),
            new IsoCountry("MC", "Monaco"),
            new IsoCountry("MN", "Mongolia"),
            new IsoCountry("ME", "Montenegro"),
            new IsoCountry("MS", "Montserrat"),
            new IsoCountry("MA", "Morocco"),
            new IsoCountry("MZ", "Mozambique"),
            new IsoCountry("MM", "Myanmar"),
            new IsoCountry("NA", "Namibia"),
            new IsoCountry("NR", "Nauru"),
            new IsoCountry("NP", "Nepal"),
            new IsoCountry("NL", "Netherlands"),
            new IsoCountry("AN", "Netherlands Antilles"),
            new IsoCountry("NC", "New Caledonia"),
            new IsoCountry("NZ", "New Zealand"),
            new IsoCountry("NI", "Nicaragua"),
            new IsoCountry("NE", "Niger"),
            new IsoCountry("NG", "Nigeria"),
            new IsoCountry("NU", "Niue"),
            new IsoCountry("NF", "Norfolk Island"),
            new IsoCountry("MP", "Northern Mariana Islands"),
            new IsoCountry("NO", "Norway"),
            new IsoCountry("OM", "Oman"),
            new IsoCountry("PK", "Pakistan"),
            new IsoCountry("PW", "Palau"),
            new IsoCountry("PS", "Palestinian Territory"),
            new IsoCountry("PA", "Panama"),
            new IsoCountry("PG", "Papua New Guinea"),
            new IsoCountry("PY", "Paraguay"),
            new IsoCountry("PE", "Peru"),
            new IsoCountry("PH", "Philippines"),
            new IsoCountry("PN", "Pitcairn"),
            new IsoCountry("PL", "Poland"),
            new IsoCountry("PT", "Portugal"),
            new IsoCountry("PR", "Puerto Rico"),
            new IsoCountry("QA", "Qatar"),
            new IsoCountry("RE", "Reunion"),
            new IsoCountry("RO", "Romania"),
            new IsoCountry("RU", "Russian Federation"),
            new IsoCountry("RW", "Rwanda"),
            new IsoCountry("BL", "Saint Barthélemy"),
            new IsoCountry("SH", "Saint Helena"),
            new IsoCountry("KN", "Saint Kitts and Nevis"),
            new IsoCountry("LC", "Saint Lucia"),
            new IsoCountry("MF", "Saint Martin"),
            new IsoCountry("PM", "Saint Pierre and Miquelon"),
            new IsoCountry("VC", "Saint Vincent and The Grenadines"),
            new IsoCountry("WS", "Samoa"),
            new IsoCountry("SM", "San Marino"),
            new IsoCountry("ST", "Sao Tome and Principe"),
            new IsoCountry("SA", "Saudi Arabia"),
            new IsoCountry("SN", "Senegal"),
            new IsoCountry("RS", "Serbia"),
            new IsoCountry("CS", "Serbia and Montenegro"),
            new IsoCountry("SC", "Seychelles"),
            new IsoCountry("SL", "Sierra Leone"),
            new IsoCountry("SG", "Singapore"),
            new IsoCountry("SK", "Slovakia"),
            new IsoCountry("SI", "Slovenia"),
            new IsoCountry("SB", "Solomon Islands"),
            new IsoCountry("SO", "Somalia"),
            new IsoCountry("ZA", "South Africa"),
            new IsoCountry("GS", "South Georgia and the South Sandwich Islands"),
            new IsoCountry("SU", "Soviet Union"),
            new IsoCountry("ES", "Spain"),
            new IsoCountry("LK", "Sri Lanka"),
            new IsoCountry("SD", "Sudan"),
            new IsoCountry("SR", "Suriname"),
            new IsoCountry("SJ", "Svalbard and Jan Mayen"),
            new IsoCountry("SZ", "Swaziland"),
            new IsoCountry("SE", "Sweden"),
            new IsoCountry("CH", "Switzerland"),
            new IsoCountry("SY", "Syrian Arab Republic"),
            new IsoCountry("TW", "Taiwan"),
            new IsoCountry("TJ", "Tajikistan"),
            new IsoCountry("TZ", "Tanzania, United Republic of"),
            new IsoCountry("TH", "Thailand"),
            new IsoCountry("TL", "Timor-Leste"),
            new IsoCountry("TG", "Togo"),
            new IsoCountry("TK", "Tokelau"),
            new IsoCountry("TO", "Tonga"),
            new IsoCountry("TT", "Trinidad and Tobago"),
            new IsoCountry("TN", "Tunisia"),
            new IsoCountry("TR", "Turkey"),
            new IsoCountry("TM", "Turkmenistan"),
            new IsoCountry("TC", "Turks and Caicos Islands"),
            new IsoCountry("TV", "Tuvalu"),
            new IsoCountry("UG", "Uganda"),
            new IsoCountry("UA", "Ukraine"),
            new IsoCountry("AE", "United Arab Emirates"),
            new IsoCountry("GB", "United Kingdom"),
            new IsoCountry("US", "United States"),
            new IsoCountry("UM", "United States Minor Outlying Islands"),
            new IsoCountry("XU", "[Unknown Country]"),
            new IsoCountry("UY", "Uruguay"),
            new IsoCountry("UZ", "Uzbekistan"),
            new IsoCountry("VU", "Vanuatu"),
            new IsoCountry("VA", "Vatican City State (Holy See)"),
            new IsoCountry("VE", "Venezuela"),
            new IsoCountry("VN", "Viet Nam"),
            new IsoCountry("VG", "Virgin Islands, British"),
            new IsoCountry("VI", "Virgin Islands, U.S."),
            new IsoCountry("WF", "Wallis and Futuna Islands"),
            new IsoCountry("EH", "Western Sahara"),
            new IsoCountry("XW", "[Worldwide]"),
            new IsoCountry("YE", "Yemen"),
            new IsoCountry("YU", "Yugoslavia"),
            new IsoCountry("ZM", "Zambia"),
            new IsoCountry("ZW", "Zimbabwe")
        };

        public static IsoCountry Find(string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }
            else if (value.Length == 2)
            {
                return All.SingleOrDefault(l => l.TwoLetterCode.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            }
            else if (value.Length == 3)
            {
                return All.SingleOrDefault(l => l.TwoLetterCode.Equals(value.Substring(0, 2), StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                return All.SingleOrDefault(l => l.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}
