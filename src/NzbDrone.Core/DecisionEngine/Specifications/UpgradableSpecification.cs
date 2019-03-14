using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Profiles.Languages;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public interface IUpgradableSpecification
    {
        bool IsUpgradable(QualityProfile profile, LanguageProfile languageProfile, List<QualityModel> currentQualities, List<Language> currentLanguages, int currentScore, QualityModel newQuality, Language newLanguage, int newScore);
        bool IsUpgradable(QualityProfile profile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality, Language newLanguage, int newScore);
        bool IsQualityUpgradable(QualityProfile profile, List<QualityModel> currentQualities, QualityModel newQuality = null);
        bool QualityCutoffNotMet(QualityProfile profile, QualityModel currentQuality, QualityModel newQuality = null);
        bool LanguageCutoffNotMet(LanguageProfile languageProfile, Language currentLanguage);
        bool CutoffNotMet(QualityProfile profile, LanguageProfile languageProfile, List<QualityModel> currentQualities, List<Language> currentLanguages, int currentScore, QualityModel newQuality = null, int newScore = 0);
        bool CutoffNotMet(QualityProfile profile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality = null, int newScore = 0);
        bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality);
        bool IsUpgradeAllowed(QualityProfile qualityProfile, LanguageProfile languageProfile, List<QualityModel> currentQualities, List<Language> currentLanguages, QualityModel newQuality, Language newLanguage);
        bool IsUpgradeAllowed(QualityProfile qualityProfile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality, Language newLanguage);
    }

    public class UpgradableSpecification : IUpgradableSpecification,
        IHandle<ModelEvent<QualityProfile>>
    {
        private readonly Logger _logger;
        private readonly ICached<Dictionary<int, double>> _qualityScoreCache;

        public UpgradableSpecification(Logger logger,
                                       ICacheManager cacheManager)
        {
            _logger = logger;
            _qualityScoreCache = cacheManager.GetCache<Dictionary<int, double>>(GetType());
        }

        private bool IsLanguageUpgradable(LanguageProfile profile, List<Language> currentLanguages, Language newLanguage = null) 
        {
            if (newLanguage != null)
            {
                var totalCompare = 0;

                foreach (var language in currentLanguages)
                {
                    var compare = new LanguageComparer(profile).Compare(newLanguage, language);
                    
                    totalCompare += compare;

                    // Not upgradable if new language is a downgrade for any current lanaguge
                    if (compare < 0)
                    {
                        return false;
                    }
                }

                // Not upgradable if new language is equal to all current languages
                if (totalCompare == 0)
                {
                    return false;
                }
            }
            return true;
        }

        private Dictionary<int, double> QualityScores(QualityProfile profile)
        {
            // use 1 based indices so max score is one
            var maxIndex = (double) profile.GetIndex(profile.LastAllowedQuality()).Index + 1;
            var result = Quality.All.ToDictionary(x => x.Id, x => (profile.GetIndex(x, true).Index + 1) / maxIndex);
            
            return result;
        }

        private double AggregateQualityScore(QualityProfile profile, IEnumerable<QualityModel> qualities)
        {
            var qualityScores = _qualityScoreCache.Get(profile.Id.ToString(), () => QualityScores(profile), TimeSpan.FromSeconds(30));

            double result = 0;
            double count = 0;
            foreach (var quality in qualities)
            {
                result += qualityScores[quality.Quality.Id];
                count += 1;
            }
            
            return result / count;
        }

        public bool IsQualityUpgradable(QualityProfile profile, List<QualityModel> currentQualities, QualityModel newQuality = null)
        {
            if (newQuality != null)
            {
                var currentScore = AggregateQualityScore(profile, currentQualities);
                var newScore = AggregateQualityScore(profile, new List<QualityModel> { newQuality });
                
                _logger.Debug($"Current quality score {currentScore:0.000} vs new quality score {newScore:0.000}");
                if (currentScore >= newScore)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPreferredWordUpgradable(int currentScore, int newScore)
        {
            return newScore > currentScore;
        }

        public bool IsUpgradable(QualityProfile qualityProfile, LanguageProfile languageProfile, List<QualityModel> currentQualities, List<Language> currentLanguages, int currentScore, QualityModel newQuality, Language newLanguage, int newScore)
        {
            if (IsQualityUpgradable(qualityProfile, currentQualities, newQuality))
            {
                return true;
            }

            foreach (var quality in currentQualities)
            {
                if (new QualityModelComparer(qualityProfile).Compare(newQuality, quality) < 0)
                {
                    _logger.Debug("Existing item has better quality, skipping");
                    return false;
                }
            }

            if (IsLanguageUpgradable(languageProfile, currentLanguages, newLanguage))
            {
                return true;
            }

            foreach (var language in currentLanguages)
            {
                if (new LanguageComparer(languageProfile).Compare(newLanguage, language) < 0)
                {
                    _logger.Debug("Existing item has better language, skipping");
                    return false;
                }
            }

            if (!IsPreferredWordUpgradable(currentScore, newScore))
            {
                _logger.Debug("Existing item has a better preferred word score, skipping");
                return false;
            }

            return true;
        }

        public bool IsUpgradable(QualityProfile qualityProfile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality, Language newLanguage, int newScore)
        {
            if (IsQualityUpgradable(qualityProfile, new List<QualityModel> { currentQuality }, newQuality))
            {
                return true;
            }

            if (new QualityModelComparer(qualityProfile).Compare(newQuality, currentQuality) < 0)
            {
                _logger.Debug("Existing item has better quality, skipping");
                return false;
            }

            if (IsLanguageUpgradable(languageProfile, new List<Language> { currentLanguage }, newLanguage))
            {
                return true;
            }

            if (new LanguageComparer(languageProfile).Compare(newLanguage, currentLanguage) < 0)
            {
                _logger.Debug("Existing item has better language, skipping");
                return false;
            }

            if (!IsPreferredWordUpgradable(currentScore, newScore))
            {
                _logger.Debug("Existing item has a better preferred word score, skipping");
                return false;
            }

            return true;
        }

        public bool QualityCutoffNotMet(QualityProfile profile, QualityModel currentQuality, QualityModel newQuality = null)
        {
            var qualityCompare = new QualityModelComparer(profile).Compare(currentQuality.Quality.Id, profile.Cutoff);

            if (qualityCompare < 0)
            {
                return true;
            }

            if (qualityCompare == 0 && newQuality != null && IsRevisionUpgrade(currentQuality, newQuality))
            {
                return true;
            }

            return false;
        }

        public bool LanguageCutoffNotMet(LanguageProfile languageProfile, Language currentLanguage)
        {
            var languageCompare = new LanguageComparer(languageProfile).Compare(currentLanguage, languageProfile.Cutoff);

            return languageCompare < 0;
        }

        public bool CutoffNotMet(QualityProfile profile, LanguageProfile languageProfile, List<QualityModel> currentQualities, List<Language> currentLanguages, int currentScore, QualityModel newQuality = null, int newScore = 0)
        {
            // If we can upgrade the language (it is not the cutoff) then the quality doesn't
            // matter as we can always get same quality with prefered language.
            foreach (var language in currentLanguages)
            {
                if (LanguageCutoffNotMet(languageProfile, language))
                {
                    return true;
                }
            }

            foreach (var quality in currentQualities)
            {
                if (QualityCutoffNotMet(profile, quality, newQuality))
                {
                    return true;
                }
            }

            if (IsPreferredWordUpgradable(currentScore, newScore))
            {
                return true;
            }

            _logger.Debug("Existing item meets cut-off. skipping.");

            return false;
        }

        public bool CutoffNotMet(QualityProfile profile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, int currentScore, QualityModel newQuality = null, int newScore = 0)
        {
            // If we can upgrade the language (it is not the cutoff) then the quality doesn't
            // matter as we can always get same quality with prefered language.
            if (LanguageCutoffNotMet(languageProfile, currentLanguage))
            {
                return true;
            }

            if (QualityCutoffNotMet(profile, currentQuality, newQuality))
            {
                return true;
            }

            if (IsPreferredWordUpgradable(currentScore, newScore))
            {
                return true;
            }

            _logger.Debug("Existing item meets cut-off. skipping.");

            return false;
        }

        public bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality)
        {
            var compare = newQuality.Revision.CompareTo(currentQuality.Revision);

            // Comparing the quality directly because we don't want to upgrade to a proper for a webrip from a webdl or vice versa
            if (currentQuality.Quality == newQuality.Quality && compare > 0)
            {
                _logger.Debug("New quality is a better revision for existing quality");
                return true;
            }

            return false;
        }

        public bool IsUpgradeAllowed(QualityProfile qualityProfile, LanguageProfile languageProfile, List<QualityModel> currentQualities, List<Language> currentLanguages, QualityModel newQuality, Language newLanguage)
        {
            var isQualityUpgrade = IsQualityUpgradable(qualityProfile, currentQualities, newQuality);
            var isLanguageUpgrade = IsLanguageUpgradable(languageProfile, currentLanguages, newLanguage);

            return CheckUpgradeAllowed(qualityProfile,languageProfile,isQualityUpgrade,isLanguageUpgrade);
        }

        public bool IsUpgradeAllowed(QualityProfile qualityProfile, LanguageProfile languageProfile, QualityModel currentQuality, Language currentLanguage, QualityModel newQuality, Language newLanguage)
        {
            var isQualityUpgrade = new QualityModelComparer(qualityProfile).Compare(newQuality, currentQuality) > 0;
            var isLanguageUpgrade = new LanguageComparer(languageProfile).Compare(newLanguage, currentLanguage) > 0;

            return CheckUpgradeAllowed(qualityProfile, languageProfile, isQualityUpgrade, isLanguageUpgrade);
        }

        private bool CheckUpgradeAllowed (QualityProfile qualityProfile, LanguageProfile languageProfile, bool isQualityUpgrade, bool isLanguageUpgrade)
        {
            if (isQualityUpgrade && qualityProfile.UpgradeAllowed ||
                isLanguageUpgrade && languageProfile.UpgradeAllowed)
            {
                _logger.Debug("At least one profile allows upgrading");
                return true;
            }

            if (isQualityUpgrade && !qualityProfile.UpgradeAllowed)
            {
                _logger.Debug("Quality profile does not allow upgrades, skipping");
                return false;
            }

            if (isLanguageUpgrade && !languageProfile.UpgradeAllowed)
            {
                _logger.Debug("Language profile does not allow upgrades, skipping");
                return false;
            }

            return true;
        }
        
        public void Handle(ModelEvent<QualityProfile> message)
        {
            _qualityScoreCache.Clear();
        }
    }
}
