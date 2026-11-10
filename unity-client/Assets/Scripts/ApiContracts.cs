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

    #region Citizen Reporting (Increment 5.11)

    [Serializable]
    public class CitizenRepresentativeProfileDto
    {
        public string id;
        public string name;
        public string role;
        public string roleEn;
        public string avatar;
        public string destination;
        public string simulationNotice;
        public string[] suggestedPromptsEn;
        public string[] suggestedPromptsEs;
    }

    [Serializable]
    public class CitizenRepresentativeResponseDto
    {
        public bool success;
        public CitizenRepresentativeProfileDto representative;
        public ApiErrorDto error;
    }

    [Serializable]
    public class BoundingBoxDto
    {
        public float ymin;
        public float xmin;
        public float ymax;
        public float xmax;
        public string label;
        public float confidence;
    }

    [Serializable]
    public class SamDetectedObjectDto
    {
        public string objectId;
        public string label;
        public float confidence;
        public BoundingBoxDto box;
        public string colorHex;
    }

    [Serializable]
    public class SamAnalysisDto
    {
        public string model;
        public string status;
        public string detectedConcept;
        public BoundingBoxDto[] boxes;
        public string maskReference;
        public string maskSummary;
        public float confidence;
        public string objectId;
        public string[] diagnostics;
        public string analyzedAt;
        public string provider;
        public string disclaimer;
        public SamDetectedObjectDto[] objects;
        public string overlayPngBase64;
        public bool hasAuthenticMasks;
    }

    [Serializable]
    public class StructuredReportDto
    {
        public string model;
        public string summary;
        public string category;
        public string locationDescription;
        public string[] additionalDetails;
        public string[] missingInformation;
        public string suggestedDepartment;
        public string clarificationQuestion;
        public string interpretedAt;
        public string provider;
    }

    [Serializable]
    public class ImageEvidenceDto
    {
        public string evidenceId;
        public string fileName;
        public int fileSizeBytes;
        public string mimeType;
        public string sha256;
        public string storagePath;
        public string uploadedAt;
    }

    [Serializable]
    public class ReportLocationDto
    {
        public string corridorEntityId;
        public string description;
        public bool isApproximate;
        public string coverageStatus;
    }

    [Serializable]
    public class ReportingRepresentativeRefDto
    {
        public string id;
        public string name;
        public string role;
        public string sector;
    }

    [Serializable]
    public class NonEmergencyAssessmentDto
    {
        public bool isEmergency;
        public string emergencyNoticeEs;
        public string emergencyNoticeEn;
    }

    [Serializable]
    public class CitizenCategoryItemDto
    {
        public string id;
        public string canonicalId;
        public string nameEs;
        public string nameEn;
        public string descriptionEs;
        public string descriptionEn;
        public string simulatedDepartmentEs;
        public string simulatedDepartmentEn;
        public string defaultSamVisualTarget;
    }

    [Serializable]
    public class CitizenCategoriesResponseDto
    {
        public bool success;
        public CitizenCategoryItemDto[] categories;
        public ApiErrorDto error;
    }

    [Serializable]
    public class CitizenReportDraftDataDto
    {
        public string draftId;
        public string citizenDescription;
        public string language;
        public ReportLocationDto location;
        public ImageEvidenceDto evidence;
        public SamAnalysisDto visualAnalysis;
        public StructuredReportDto structuredInterpretation;
        public string simulatedDestination;
        public string simulationNotice;
        public bool confirmationRequired;
        public string confirmedCategory;
        public ReportingRepresentativeRefDto reportingRepresentative;
        public NonEmergencyAssessmentDto nonEmergencyAssessment;
        public bool externalAiProcessingConsent;
    }

    [Serializable]
    public class CitizenReportImageUploadDto
    {
        public string fileName;
        public string mimeType;
        public string base64;
    }

    [Serializable]
    public class CitizenReportDraftRequestDto
    {
        public string citizenDescription;
        public string locationDescription;
        public string corridorEntityId;
        public string language;
        public string category;
        public string representativeId;
        public bool externalAiProcessingConsent;
        public CitizenReportImageUploadDto image;
    }

    [Serializable]
    public class CitizenReportDraftResponseDto
    {
        public bool success;
        public CitizenReportDraftDataDto draft;
        public ApiErrorDto error;
    }

    [Serializable]
    public class CitizenReportSubmitRequestDto
    {
        public string draftId;
        public bool confirmedByPlayer;
        public string confirmedCategory;
    }

    [Serializable]
    public class CitizenReportReceiptDto
    {
        public string reportId;
        public string category;
        public string location;
        public string locationCoverage;
        public string representative;
        public string evidenceStatus;
        public string visualAnalysisStatus;
        public string simulatedDestination;
        public string lifecycleState;
        public string createdAt;
    }

    [Serializable]
    public class CitizenReportRecordDto
    {
        public string reportId;
        public string citizenDescription;
        public string language;
        public ReportLocationDto location;
        public ImageEvidenceDto evidence;
        public SamAnalysisDto visualAnalysis;
        public StructuredReportDto structuredInterpretation;
        public string lifecycleState;
        public string simulatedDestination;
        public string simulationNotice;
        public string confirmedCategory;
        public ReportingRepresentativeRefDto reportingRepresentative;
        public bool externalAiProcessingConsent;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public class CitizenReportSubmitResponseDto
    {
        public bool success;
        public CitizenReportRecordDto report;
        public CitizenReportReceiptDto receipt;
        public string simulationNotice;
        public ApiErrorDto error;
    }

    #endregion

    #region NPC Activity & Daily Routines DTOs

    [Serializable]
    public class NpcActivitySnapshotDto
    {
        public string npcId;
        public string canonicalName;
        public string state; // "available" | "busy" | "off_duty" | "unavailable"
        public BilingualTextDto activityTitle;
        public BilingualTextDto activityDescription;
        public string currentLocation;
        public BilingualTextDto locationName;
        public bool canInteract;
        public BilingualTextDto interactionNotice;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public int dayOfWeek;
        public string dayName;
        public string nextTransitionTime;
        public bool isDemonstrationSchedule;
        public BilingualTextDto demonstrationNotice;
    }

    [Serializable]
    public class NpcActivityResponseDto
    {
        public bool success;
        public NpcActivitySnapshotDto activity;
        public ApiErrorDto error;
    }

    [Serializable]
    public class NpcRosterSummaryDto
    {
        public int availableCount;
        public int busyCount;
        public int offDutyCount;
        public int unavailableCount;
        public int totalCount;
    }

    [Serializable]
    public class NpcRosterActivityResponseDto
    {
        public bool success;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public NpcRosterSummaryDto summary;
        public bool isDemonstrationSchedule;
        public BilingualTextDto demonstrationNotice;
        public ApiErrorDto error;
    }

    #endregion

    #region Environmental Presentation & World Time DTOs (Increment 5.15.0)

    [Serializable]
    public class CivilTimeDto
    {
        public int year;
        public int month;
        public int day;
        public int hour;
        public int minute;
        public int second;
        public int dayOfWeek;
    }

    [Serializable]
    public class EnvironmentalLightingDto
    {
        public string phase;
        public BilingualTextDto phaseTitle;
        public float phaseProgress;
        public bool isNight;
        public float sunPitchDegrees;
        public float sunYawDegrees;
        public float sunIntensity;
        public string sunColorHex;
        public float ambientIntensity;
        public string ambientSkyHex;
        public string ambientEquatorHex;
        public string ambientGroundHex;
        public bool streetlightsActive;
        public float streetlightsIntensity;
        public float skyboxExposure;
        public string fogColorHex;
    }

    [Serializable]
    public class WorldEnvironmentResponseDto
    {
        public bool success;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public CivilTimeDto civilTime;
        public EnvironmentalLightingDto environment;
        public bool isDemonstrationPresentation;
        public BilingualTextDto demonstrationNotice;
        public ApiErrorDto error;
    }

    #endregion

    #region NPC Waypoint Movement & Spatial Presence DTOs (Increment 5.16.0)

    [Serializable]
    public class Vector3Dto
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class HeadingDto
    {
        public float x;
        public float z;
    }

    [Serializable]
    public class NpcMovementSnapshotDto
    {
        public string npcId;
        public string canonicalName;
        public string movementState; // "stationary" | "moving" | "arrived" | "waiting"
        public string currentWaypointId;
        public string destinationWaypointId;
        public string activeRouteId;
        public Vector3Dto position;
        public HeadingDto forwardHeading;
        public float progression;
        public string movementStartTime;
        public float expectedDurationSeconds;
        public string associatedPlaceId;
        public string activityState;
        public BilingualTextDto activityTitle;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public bool isDemonstrationMovement;
        public BilingualTextDto demonstrationNotice;
    }

    [Serializable]
    public class NpcMovementResponseDto
    {
        public bool success;
        public NpcMovementSnapshotDto movement;
        public ApiErrorDto error;
    }

    [Serializable]
    public class NpcMovementSummaryDto
    {
        public int stationaryCount;
        public int movingCount;
        public int arrivedCount;
        public int waitingCount;
        public int totalCount;
    }

    [Serializable]
    public class NpcRosterMovementResponseDto
    {
        public bool success;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public NpcMovementSummaryDto summary;
        public NpcMovementSnapshotDto[] npcsList;
        public bool isDemonstrationMovement;
        public BilingualTextDto demonstrationNotice;
        public ApiErrorDto error;
    }

    #endregion

    #region NPC Interaction Context & Spatial Demeanor DTOs (Increment 5.17.0)

    [Serializable]
    public class NpcInteractionContextDto
    {
        public string npcId;
        public string canonicalName;
        public BilingualTextDto role;
        public string activityState;
        public BilingualTextDto activityTitle;
        public BilingualTextDto activityDescription;
        public string interactionAvailability; // available_normal | available_in_transit | available_contextual | busy | off_duty | unavailable
        public string interactionMode;         // normal_service | in_transit | break_or_dwell | busy_priority | off_duty_courtesy | closed
        public bool canInitiateConversation;
        public bool isAvailableForNormalService;
        public bool isMoving;
        public string movementState;
        public string currentWaypointId;
        public BilingualTextDto currentWaypointLabel;
        public string associatedPlaceId;
        public string destinationWaypointId;
        public BilingualTextDto destinationWaypointLabel;
        public string activeRouteId;
        public float progression;
        public BilingualTextDto decisionReason;
        public BilingualTextDto dialogueGuidance;
        public BilingualTextDto contextualGreeting;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public bool isDemonstrationContext;
        public BilingualTextDto demonstrationNotice;
    }

    [Serializable]
    public class NpcInteractionContextResponseDto
    {
        public bool success;
        public NpcInteractionContextDto context;
        public NpcInteractionContextDto interactionContext;
        public ApiErrorDto error;
    }

    [Serializable]
    public class NpcInteractionSummaryDto
    {
        public int availableNormalCount;
        public int inTransitCount;
        public int contextualCount;
        public int busyCount;
        public int offDutyCount;
        public int unavailableCount;
        public int totalCount;
    }

    [Serializable]
    public class NpcRosterInteractionContextResponseDto
    {
        public bool success;
        public string evaluatedAt;
        public string timeZone;
        public string localCivilTime;
        public NpcInteractionSummaryDto summary;
        public NpcInteractionContextDto[] npcsList;
        public bool isDemonstrationContext;
        public BilingualTextDto demonstrationNotice;
        public ApiErrorDto error;
    }

    #endregion

    #region Border Lane Toll Barriers DTOs

    [Serializable]
    public class BorderLaneBarrierDto
    {
        public string laneId;
        public int archNumber;
        public string name;
        public string status; // "open" | "closed"
        public float angleDegrees;
        public string reason;
        public string updatedAt;
    }

    [Serializable]
    public class BorderLaneBarriersResponseDto
    {
        public bool success;
        public string worldTime;
        public BorderLaneBarrierDto[] lanesList;
        public ApiErrorDto error;
    }

    [Serializable]
    public class BorderLaneBarrierMutationRequestDto
    {
        public string status; // "open" | "closed"
        public string reason;
    }

    [Serializable]
    public class BorderLaneBarrierMutationResponseDto
    {
        public bool success;
        public BorderLaneBarrierDto lane;
        public ApiErrorDto error;
    }

    #endregion
}
