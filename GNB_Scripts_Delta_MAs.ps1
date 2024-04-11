# @author: Reynaldo Jimenez
# @mail: djimenez **at** ksconsultingmx.com
# including run-history if things go wrong- djimenez **at** ksconsultingmx.com


############
# PARAMETERS
############
$params_ComputerName = "."  # "." is the current computer
$params_delayBetweenExecs = 60 #delay between each execution, in seconds
$params_numOfExecs = 1    #Number of executions 0 for infinite
$params_runProfilesOrder = 
@(

 #inicia sincronizacion empleados
 

 <# #>

   @{
 name="MIM_GNB_MIM_MA";
 profilesToRun=@("Delta Import","Delta Synchronization");
 };

   @{
 name="MIM_GNB_ADDS_MA"; 
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };

 @{
 name="MIM_GNB_ADDS_EXTERNOS_MA";
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };

 <#
  @{
 name="MIM_GNB_EC_MA_1"; 
 profilesToRun=@("Full Import","Ful Synchronization");
 };

 #>
  @{
 name="MIM_GNB_EC_MA_1"; 
 profilesToRun=@("Delta Import2","Delta Import2","Delta Synchronization");
 };
 

  @{
 name="MIM_GNB_MIM_MA";
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };

 
 @{
 name="MIM_GNB_ADDS_MA"; 
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };

 @{
 name="MIM_GNB_ADDS_EXTERNOS_MA";
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };

  @{
 name="MIM_GNB_EC_MA_1"; 
 profilesToRun=@("Export");
 };

   @{
 name="MIM_GNB_MIM_MA";
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };

 
   @{
 name="MIM_GNB_SAPECC_MA";
 profilesToRun=@("Export","Full Import","Full Sync");
 };

 <#   @{
 name="MIM_GNB_SAPECC_MA";
 profilesToRun=@("Full Import","Full Sync");
 };#>


   @{
 name="MIM_GNB_OFFICE365_MA";
 profilesToRun=@("Full Import","Full Sync","Export");
 };


    @{
 name="MIM_GNB_AD_PASSWORD_EXPIRE_MA";
 profilesToRun=@("Full Import","Full Synchronization");
 };

 
    @{
 name="MIM_GNB_AD_PASSWORD_EXPIRE_EXTERNOS_MA";
 profilesToRun=@("Full Import","Full Synchronization");
 };

    @{
 name="MIM_GNB_MIM_MA";
 profilesToRun=@("Export","Delta Import","Delta Synchronization");
 };


);
##################
# EMAIL PARAMETERS
##################
$emailFrom = ""
$emailTo = ""
$smtpServer = ""

############
# FUNCTIONS
############
$line = "-----------------------------"
function Write-Output-Banner([string]$msg) {
 Write-Output $line,$msg,$line
}

############
# DATAS
############

$MAs = @(get-wmiobject -class "MIIS_ManagementAgent" -namespace "root\MicrosoftIdentityIntegrationServer" -computername $params_ComputerName)
$numOfExecDone = 0

############
# PROGRAM
############
do {
 Write-Output-Banner("Execution #:"+(++$numOfExecDone))
 foreach($MATypeNRun in $params_runProfilesOrder) {
 $found = $false;
 foreach($MA in $MAS) { 
 
   if(!$found) {
#  if($MA.Type.Equals($MATypeNRun.type)) {
  if($MA.Name.Equals($MATypeNRun.name)) {
  $found=$true;
  Write-Output-Banner("- MA Name: "+$MA.Name,"`n- Type: "+$MA.Type)
#  Write-Output-Banner("MA Type: "+$MA.Type)
  foreach($profileName in $MATypeNRun.profilesToRun) {
    
    if($MA.Name.Equals("MIM_GNB_MIM_MA") -and ($profileName.Equals("Delta Import") -or $profileName.Equals("Full Import")))
      {
        Start-Sleep -Seconds 600
      }
   Write-Output (" "+$profileName)," -> starting"
   $datetimeBefore = Get-Date;
   $result = $MA.Execute($profileName);
   $datetimeAfter = Get-Date;
   $duration = $datetimeAfter - $datetimeBefore;
   if("success".Equals($result.ReturnValue)){
   $msg = "done. Duration: "+$duration.Hours+":"+$duration.Minutes+":"+$duration.Seconds
   } else { 
   $msg = "Error: "+$result 
# Problems with RunHistory WMI not working with MaGuid or MaName, so I used RunDetails from the MA.
#   $RunHistory = get-wmiobject -class "MIIS_RunHistory" -namespace "root\MicrosoftIdentityIntegrationServer" -filter("MaGuid='" + $MA.guid + "'")
#   # Write the RunHistory file XML out to a file to then attach to the e-mail, and also set it to a XML attribute.
#   $RunHistory[1].RunDetails().ReturnValue | Out-File RunHistory.xml 
#   Grab the first run-history, which always is the latest result.
#   [xml]$RunHistoryXML = $RunHistory[1].RunDetails().ReturnValue
   #Take the MA RunDetails RunHistory XML and write to file.
   $MA.RunDetails().ReturnValue | Out-File RunHistory.xml
   # Grab the MA run-history and put it into a XML var.
   [xml]$RunHistoryXML = $MA.RunDetails().ReturnValue
   # Build User Errors for Exports
   $RunHistoryXML."run-history"."run-details"."step-details"."synchronization-errors"."export-error" | select dn,"error-type" | export-csv ErrorUsers.csv
   
   }
   
   Write-Output (" -> "+$result.ReturnValue)
  }
  }
   }
 }
 if(!$found) { Write-Output ("Not found MA type :"+$MATypeNRun.type); }
 }
 $continue = ($params_numOfExecs -EQ 0) -OR ($numOfExecDone -lt $params_numOfExecs)
 if($continue) { 
 Write-Output-Banner("Sleeping "+$params_delayBetweenExecs+" seconds")
 Start-Sleep -s $params_delayBetweenExecs
 }
} while($continue)
