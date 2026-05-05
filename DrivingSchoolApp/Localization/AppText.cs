namespace DrivingSchoolApp.Localization;

public static class AppText
{
    public static string ShellRouteTabTitle => "Rute";
    public static string ShellSavedTabTitle => "Gemte";
    public static string ShellLoginTabTitle => "Log ind";

    public static string LiveRoutePageTitle => "Rute";
    public static string LiveCenterButton => "Centrer";
    public static string LiveStartButton => "Start";
    public static string LiveResumeButton => "Genoptag";
    public static string LiveStopButton => "Pause";
    public static string LiveEndButton => "Afslut";
    public static string LiveResetButton => "Nulstil";
    public static string LiveWaitingForGpsTitle => "Venter på første GPS-punkt";
    public static string LiveWaitingForGpsMessage => "Hold appen åben et øjeblik, mens telefonen finder en gyldig position.";
    public static string LiveLocationRequiredTitle => "Lokation kræves";
    public static string LiveLocationRequiredMessage => "Lokationstilladelse er nødvendig for at starte rutesporing.";
    public static string LiveTrackingErrorTitle => "Sporingsfejl";
    public static string LiveTrackingErrorMessage => "Baggrundssporing kunne ikke startes.";
    public static string LiveEndRouteTitle => "Afslut rute?";
    public static string LiveEndRouteMessage => "Dette vil afslutte og gemme den aktuelle rute.";
    public static string LiveEndRouteConfirm => "Afslut rute";
    public static string LiveResetRouteTitle => "Nulstil rute?";
    public static string LiveResetRouteMessage => "Dette vil kassere den aktuelle rute og slette alle indsamlede punkter.";
    public static string LiveResetRouteConfirm => "Nulstil rute";
    public static string LiveBackgroundPrompt =>
        "For bedre rutesporing når skærmen slukker, kan du åbne appindstillingerne og tillade baggrundslokation, hvis din telefon understøtter det.";
    public static string LiveBackgroundPromptLater => "Ikke nu";
    public static string LiveBackgroundPromptOpenSettings => "Åbn indstillinger";

    public static string LoginPageTitle => "Log ind";
    public static string LoginHeader => "Log ind";
    public static string LoginEmailPlaceholder => "E-mail";
    public static string LoginPasswordPlaceholder => "Adgangskode";
    public static string LoginButton => "Log ind";
    public static string LoginForgotPassword => "Glemt adgangskode?";
    public static string LoginMissingInfoTitle => "Manglende oplysninger";
    public static string LoginMissingInfoMessage => "Indtast både e-mail og adgangskode.";
    public static string LoginSuccessTitle => "Klar";
    public static string LoginSuccessMessage => "Du er klar til at bruge appen.";
    public static string LoginSuccessButton => "Fint";

    public static string SavedRoutesPageTitle => "Gemte ruter";
    public static string SavedRoutesEdit => "Rediger";
    public static string SavedRoutesDone => "Færdig";
    public static string SavedRoutesDelete => "Slet";
    public static string SavedRoutesEmptyTitle => "Ingen gemte ruter endnu";
    public static string SavedRoutesEmptyMessage => "Afslut en rute fra live-skærmen, så vises den her.";
    public static string SavedRoutesDeleteTitle => "Slet ruter";
    public static string SavedRoutesDeleteSelectMessage => "Vælg mindst én rute først.";
    public static string SavedRoutesDeleteConfirmTitle => "Slet valgte ruter?";

    public static string RouteReviewPageTitle => "Lektionsgennemgang";
    public static string RouteReviewSummaryHeader => "Lektionsoversigt";
    public static string RouteReviewStartLabel => "Start";
    public static string RouteReviewEndLabel => "Slut";
    public static string RouteReviewDurationLabel => "Varighed";
    public static string RouteReviewDistanceLabel => "Distance";
    public static string RouteReviewPreviewHeader => "Ruteoversigt";
    public static string RouteReviewPreviewMessage => "Forhåndsvisningen bruger de indsamlede GPS-punkter fra denne lektion.";
    public static string RouteReviewPeopleHeader => "Deltagere";
    public static string RouteReviewStudentLabel => "Elev";
    public static string RouteReviewStudentPlaceholder => "Elevens fulde navn";
    public static string RouteReviewInstructorLabel => "Underviser";
    public static string RouteReviewInstructorPlaceholder => "Underviserens navn";
    public static string RouteReviewSkillsHeader => "Færdigheder gennemført i lektionen";
    public static string RouteReviewAllCompletedMessage => "Alle registrerede lektionsemner i kataloget er allerede gennemført for denne elev.";
    public static string RouteReviewInstructorSignoffHeader => "Underviserens godkendelse";
    public static string RouteReviewClearInstructorSignature => "Ryd underviserens underskrift";
    public static string RouteReviewInstructorSignaturePrompt => "Tegn underviserens underskrift ovenfor.";
    public static string RouteReviewInstructorSignatureCaptured => "Underviserens underskrift er registreret.";
    public static string RouteReviewStudentSignoffHeader => "Elevens godkendelse";
    public static string RouteReviewClearStudentSignature => "Ryd elevens underskrift";
    public static string RouteReviewStudentSignaturePrompt => "Tegn elevens underskrift ovenfor.";
    public static string RouteReviewStudentSignatureCaptured => "Elevens underskrift er registreret.";
    public static string RouteReviewFinalizeButton => "Afslut lektion";
    public static string RouteReviewNotFoundTitle => "Lektion ikke fundet";
    public static string RouteReviewNotFoundMessage => "Lektionssessionen kunne ikke indlæses.";
    public static string RouteReviewMissingStudentTitle => "Elev mangler";
    public static string RouteReviewMissingStudentMessage => "Indtast elevens navn, før lektionen afsluttes.";
    public static string RouteReviewMissingInstructorTitle => "Underviser mangler";
    public static string RouteReviewMissingInstructorMessage => "Indtast underviserens navn, før lektionen afsluttes.";
    public static string RouteReviewInstructorSignatureRequiredTitle => "Underviserens underskrift kræves";
    public static string RouteReviewInstructorSignatureRequiredMessage => "Underviseren skal underskrive lektionen.";
    public static string RouteReviewStudentSignatureRequiredTitle => "Elevens underskrift kræves";
    public static string RouteReviewStudentSignatureRequiredMessage => "Eleven skal underskrive lektionen.";
    public static string RouteReviewFinalizedTitle => "Lektion afsluttet";
    public static string RouteReviewFinalizedMessage => "Lektionsgennemgang og godkendelse er gemt.";

    public static string RouteDetailPageTitle => "Gemt rute";
    public static string RouteDetailSnapButton => "Tilpas";
    public static string RouteDetailRetrySnapButton => "Prøv igen";
    public static string RouteDetailResnapButton => "Tilpas igen";
    public static string RouteDetailSnappingInProgressButton => "Tilpasser...";
    public static string RouteDetailDeleteButton => "Slet";
    public static string RouteDetailLoadingMessage => "Indlæser rute...";
    public static string RouteDetailSnappingMessage => "Tilpasser rute...";
    public static string RouteDetailResnappingMessage => "Tilpasser rute igen...";
    public static string RouteDetailNotFoundTitle => "Rute ikke fundet";
    public static string RouteDetailNotFoundMessage => "Den gemte rute kunne ikke indlæses.";
    public static string RouteDetailSnapUnavailableTitle => "Tilpasning utilgængelig";
    public static string RouteDetailSnappedTitle => "Rute tilpasset";
    public static string RouteDetailSnappedMessage => "Den gemte rute er forbedret og gemt til offline visning.";
    public static string RouteDetailDeleteTitle => "Slet rute?";
    public static string RouteDetailDeleteMessage => "Den gemte rute vil blive slettet.";
    public static string RouteDetailStartPin => "Start";
    public static string RouteDetailEndPin => "Slut";

    public static string StudentProgressPrompt => "Indtast elevens navn for at indlæse tidligere gennemførte færdigheder.";
    public static string StudentProgressNoHistory => "Ingen tidligere gennemførte færdigheder fundet for denne elev.";
    public static string StudentProgressEmpty => "Denne elev har endnu ingen gennemførte lektionsemner.";
    public static string StudentProgressCompletedPrefix => "Allerede gennemført: ";

    public static string SavedRouteStatusNeedsSignOff => "Mangler godkendelse";
    public static string SavedRouteStatusMapProcessing => "Behandler kort";
    public static string SavedRouteStatusSnapFailed => "Tilpasning mislykkedes";
    public static string SavedRouteStatusReadyToView => "Klar til visning";
    public static string SavedRouteNoStudentAssigned => "Ingen elev valgt";

    public static string CommonOk => "OK";
    public static string CommonCancel => "Annuller";

    public static string FormatSavedRoutesDeleteCount(int count) => $"Slet {count} gemte rute(r)?";

    public static string FormatSavedRouteSubtitle(string studentName, string duration, int pointCount)
        => $"{studentName} - {duration} - {pointCount} punkter";
}
