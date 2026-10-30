using System;

namespace MyVirtualCommunity.Client.Api
{
    #region Server Health & Core DTOs

    [Serializable]
    public class WorldCoreHealthDto
    {
        public string mode;
        public string reachability;
        public bool connected;
        public int latencyMs;
    }

    [Serializable]
    public class HealthResponseDto
    {
        public string status;
        public string service;
        public bool metaModelConfigured;
        public string model;
        public string metaProvider;
        public WorldCoreHealthDto worldCore;
        public string timestamp;
    }

    [Serializable]
    public class BilingualTextDto
    {
        public string en;
        public string es;

        public string Get(string language)
        {
            if (string.Equals(language, "es", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(es) ? es : en;
            }
            return !string.IsNullOrEmpty(en) ? en : es;
        }
    }

    [Serializable]
    public class GeoCoordinatesDto
    {
        public double latitude;
        public double longitude;
    }

    [Serializable]
    public class SpatialEntityDto
    {
        public string id;
        public string entityType;
        public string canonicalName;
        public string popularName;
        public string address;
        public GeoCoordinatesDto coordinates;
        public string businessId;
        public BilingualTextDto description;
    }

    [Serializable]
    public class SpatialCorridorDto
    {
        public string id;
        public BilingualTextDto name;
        public string city;
        public string state;
        public string country;
        public string timeZone;
        public GeoCoordinatesDto center;
        public int defaultZoom;
        public SpatialEntityDto[] entities;
    }

    [Serializable]
    public class SpatialCorridorResponseDto
    {
        public bool success;
        public SpatialCorridorDto corridor;
    }

    [Serializable]
    public class ApiErrorDto
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class ApiErrorResponseDto
    {
        public bool success;
        public ApiErrorDto error;
    }

    #endregion

    #region Official Border Wait Times DTOs (Increment 1 & 5.3)

    [Serializable]
    public class BorderCrossingObservationDto
    {
        public string crossingId;
        public string portNumber;
        public string canonicalName;
        public string direction;
        public string mode;
        public string laneCategory;
        public BilingualTextDto laneCategoryName;
        public int delayMinutes;
        public bool hasDelayMinutes = true;
        public string unit;
        public string operationalStatus;
        public string portStatus; // "Open" | "Closed" | "Unknown"
        public int lanesOpen;
        public int maximumLanes;
        public string sourceObservationTime;
        public string sourceObservationTimestamp;
        public string fetchedAt;
        public string sourceUrl;
        public string provider;
        public string freshness; // "fresh" | "stale" | "unavailable"
        public string errorMessage;

        public string FormatDelay(string language)
        {
            bool isEs = string.Equals(language, "es", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(portStatus, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return isEs ? "Puerto Cerrado" : "Port Closed";
            }

            if (!hasDelayMinutes)
            {
                return isEs ? "No disponible / Pendiente" : "Unavailable / Pending";
            }

            return $"{delayMinutes} min";
        }
    }

    [Serializable]
    public class ObservationsContainerDto
    {
        public BorderCrossingObservationDto pasoDelNorte;
        public BorderCrossingObservationDto bridgeOfTheAmericas;
    }

    [Serializable]
    public class ComparisonContainerDto
    {
        public bool canCompare;
        public int differenceMinutes;
        public string lowerWaitCrossingId;
        public BilingualTextDto statement;
        public BilingualTextDto reasonUnavailable;
    }

    [Serializable]
    public class CameraMetadataDto
    {
        public string officialAuthorityUrl;
        public string secondaryAuthorityUrl;
        public string provider;
        public BilingualTextDto description;
    }

    [Serializable]
    public class BorderWaitTimesComparisonDto
    {
        public string direction;
        public string mode;
        public string laneCategory;
        public BilingualTextDto laneCategoryDisplay;
        public ObservationsContainerDto observations;
        public ComparisonContainerDto comparison;
        public CameraMetadataDto cameras;
        public string fetchedAt;
    }

    [Serializable]
    public class BorderWaitTimesResponseDto
    {
        public bool success;
        public BorderWaitTimesComparisonDto comparison;
        public ApiErrorDto error;
    }

    #endregion

    #region Border Concierge DTOs (Increment 3 & 5.3)

    [Serializable]
    public class ConciergeProfileDto
    {
        public string id;
        public string name;
        public BilingualTextDto role;
        public string avatar;
    }

    [Serializable]
    public class AskConciergeRequestDto
    {
        public string message;
        public string language;
    }

    [Serializable]
    public class AskConciergeResponseDto
    {
        public bool success;
        public ConciergeProfileDto concierge;
        public string userQuestion;
        public string language;
        public string intent;
        public string text;
        public string provider;
        public ObservationsContainerDto authoritativeObservations;
        public ComparisonContainerDto comparison;
        public CameraMetadataDto cameras;
        public string fetchedAt;
        public BilingualTextDto officialDataNotice;
        public ApiErrorDto error;
    }

    #endregion

    #region Synthetic Community Witnesses DTOs (Increment 2 & 5.3)

    [Serializable]
    public class SyntheticProvenanceDto
    {
        public string type;
        public BilingualTextDto marker;
        public string scenarioId;
        public string locationId;
        public string provider;
        public BilingualTextDto officialDataNotice;
    }

    [Serializable]
    public class WitnessChatResponseDataDto
    {
        public string witnessId;
        public string witnessName;
        public string avatar;
        public BilingualTextDto role;
        public string text;
        public string language;
        public SyntheticProvenanceDto provenance;
    }

    [Serializable]
    public class WitnessChatRequestDto
    {
        public string npcId;
        public string message;
        public string language;
    }

    [Serializable]
    public class WitnessChatResponseDto
    {
        public bool success;
        public WitnessChatResponseDataDto response;
        public ApiErrorDto error;
    }

    #endregion

    #region Grounded Storefront Business Attendants DTOs (Increment 4 & 5.3)

    [Serializable]
    public class AuthoritativeStorefrontFactsDto
    {
        public string businessName;
        public string businessId;
        public string address;
        public string status;
        public bool isOpen;
        public bool admissionPermitted;
        public string hoursSummary;
        public string openTimeCivil;
        public string closeTimeCivil;
        public string cutoffTimeCivil;
        public string timeZone;
        public string evaluatedAtCivil;
        public string evaluatedAtUtc;
        public string unavailableFactsNotice;
    }

    [Serializable]
    public class BusinessAttendantChatResponseDataDto
    {
        public string attendantId;
        public string businessId;
        public string businessCanonicalName;
        public string attendantName;
        public string attendantRole;
        public string text;
        public string topic;
        public string language;
        public string virtualDisclosure;
        public string provider;
        public AuthoritativeStorefrontFactsDto authoritativeFacts;
        public int latencyMs;
    }

    [Serializable]
    public class BusinessAttendantChatRequestDto
    {
        public string attendantId;
        public string message;
        public string language;
        public string evaluationTime;
    }

    [Serializable]
    public class BusinessAttendantChatResponseDto
    {
        public bool success;
        public BusinessAttendantChatResponseDataDto response;
        public ApiErrorDto error;
    }

    #endregion
}
