#!/usr/bin/perl

use File::Slurp 'edit_file';
use Time::Local;

#======================================================================================
# 0. Build Parameteres (versioned elements, paths - all collected here in one place)
#======================================================================================


my $Source_Path = $ENV {'RepPath'};
my $Temp_Path = $ENV {'InstallerTemp'};

my $MainVersionNumber = $ENV {'MainVersionNumber'};
my $Build_Number = $ENV {'BUILD_NUMBER'};

my $MainWiXGUID = "EFD34E97-291A-43FC-8232-AD0BBA786C42";

#======================================================================================
# VI. Parse WiX files
#======================================================================================

edit_file {
 
    s/%T_MainWiXGUID_T%/$MainWiXGUID/xgi;
    s/%T_MainToolVersion_T%/$MainVersionNumber/xgi;
    s/%T_BuildNumber_T%/$Build_Number/xgi;
 
} $Source_Path."/BuildScripts/WiX/SensorTool_Product.wxs";

#======================================================================================
# IV. Parse AssemblyInfo.cs Files
#====================================================================================== 

PatchAssemblyInfo ($Source_Path."/SensorTool/", $MainVersionNumber, $Build_Number);

#======================================================================================
# Appendix A: "Auxiliary Subroutines"
#======================================================================================

#-------------------------------
# Patching AssemblyInfo.cs Files
#-------------------------------
sub PatchAssemblyInfo {
    my $Source_D = shift;
    my $Version_N = shift;
    my $Build_N = shift;
    
    my @List = GetAssemblyList($Source_D);
    
    foreach my $Path (@List) {
        edit_file {
            s/(\d+[.]\d+[.]\d+)[.](\d+)/$Version_N.$Build_N/xgi;
        } $Path;
    }
    
}

#-------------------------------
# Getting AssemblyInfo paths
#-------------------------------
sub GetAssemblyList {
    my $root = shift;
    my @AssemblyInfoList;
    
    opendir(my $ROOT, $root) or die $!;
    my (@filelist) = readdir $ROOT;
    closedir $ROOT;

    for my $x (@filelist) {
        if ($x ne "." and $x ne "..") { 
            my $path = $root.'/'.$x;
            if (-f $path) {
                if ($path eq $root.'/'.'AssemblyInfo.cs') {
                    push(@AssemblyInfoList, $path);
                }
            }
            if (-d $path) { 
            push(@AssemblyInfoList, GetAssemblyList($path));
            } 
        } 
    }
    return @AssemblyInfoList;
}
