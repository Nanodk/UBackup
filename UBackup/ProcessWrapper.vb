Imports System.IO
''' <summary>
''' Run a process
''' </summary>
Public Class ProcessWrapper

#Region "Constantes"
    Const LOGNAME As String = "[PROCESS] "
    Public Shared ExePar2 As String = "par2.exe"
    Public Shared ExeNyuu As String = "nyuu.exe"
    Public Shared ExeNzbGet As String = "nzbget.exe"
    Public Shared ConfNzbGet As String = "nzbget.conf"
#End Region

#Region "Run"
    ''' <summary>
    ''' Run an external process
    ''' </summary>
    Private Shared Function Run(workingDir As String, exeLocation As String, args As String, timeoutSec As Integer, ByRef exitCode As Integer) As Boolean
        Try
            Using proc As New Process
                proc.StartInfo.Arguments = args
                proc.StartInfo.WorkingDirectory = workingDir
                proc.StartInfo.FileName = Path.Combine(workingDir, exeLocation)
                proc.StartInfo.UseShellExecute = False
                'proc.StartInfo.RedirectStandardOutput = True
                'proc.StartInfo.RedirectStandardError = True
                proc.StartInfo.CreateNoWindow = True

                proc.Start()
                proc.WaitForExit(timeoutSec * 1000)

                'strOut = proc.StandardOutput.ReadToEnd()
                'strErr = proc.StandardOutput.ReadToEnd()

                exitCode = proc.ExitCode
            End Using
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            exitCode = -1
            Return False
        End Try
        Return True
    End Function
#End Region

#Region "Par2"
    ''' <summary>
    ''' Create parity files
    ''' </summary>
    Public Shared Function Par2Create(workingDir As String, filePath As String) As Boolean
        Try
            Dim exitCode As Integer = -1
            Dim blocksize As Integer = 1024 * Config.UploadArticleSize
            Dim args As String = "create -s" & blocksize & " """ & filePath & ".par2"" """ & filePath & "*"""
            If ProcessWrapper.Run(workingDir, ExePar2, args, 3600, exitCode) = True Then
                If exitCode = 0 Then
                    Return True
                End If
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Check and Repair file
    ''' </summary>
    Public Shared Function Par2Repair(workingDir As String, par2FilePath As String) As Boolean
        Try
            Dim exitCode As Integer = -1
            Dim args As String = "repair """ & par2FilePath & """"
            If ProcessWrapper.Run(workingDir, ExePar2, args, 3600, exitCode) = True Then
                If exitCode = 0 Then
                    Return True
                End If
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function
#End Region

#Region "Nyuu"
    ''' <summary>
    ''' Upload to newsgroup server
    ''' </summary>
    Public Shared Function NyuuUpload(workingDir As String, nzb As String, folder As String) As Boolean
        Try
            Dim exitCode As Integer = -1
            Dim poster As String = Config.UploadPoster
            If String.IsNullOrEmpty(poster) Then
                poster = Utilities.RandomPoster()
            End If
            Dim args As String = "-h " & Config.NewsGroupServer & " -P " & Config.NewsGroupPort & " -S -u " & Config.NewsGroupUser & " -p " & Config.NewsGroupPassword & " -n" & Config.NewsGroupConns & " -a " & Config.UploadArticleSize & "K -F -f """ & poster & """ -g " & Config.UploadGroups & " -k 1 -o " & nzb & " " & folder
            If ProcessWrapper.Run(workingDir, ExeNyuu, args, 3600 * 12, exitCode) = True Then
                If exitCode = 0 Then
                    Return True
                End If
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function
#End Region

#Region "NzbGet"
    ''' <summary>
    ''' Download from newsgroup server
    ''' </summary>
    Public Shared Function NzbGet(workingDir As String, nzbPath As String) As Boolean
        Try
            Dim exitCode As Integer = -1
            Dim args As String = "-c """ & Path.Combine(workingDir, "nzbget.conf") & """ """ & nzbPath & """"
            If ProcessWrapper.Run(workingDir, ExeNzbGet, args, 3600 * 12, exitCode) = True Then
                If exitCode = 0 Then
                    Return True
                End If
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function
#End Region

#Region "Various"
    ''' <summary>
    ''' Kill running processing
    ''' </summary>
    Public Shared Sub KillProcesses()
        Try
            For i As Integer = 0 To 2
                Dim proc As String = vbNullString
                Select Case i
                    Case 0 : proc = ExeNyuu
                    Case 1 : proc = ExePar2
                    Case 2 : proc = ExeNzbGet
                End Select
                If String.IsNullOrEmpty(proc) = False Then
                    proc = Split(proc, ".")(0)
                    Dim procs() As Process = Process.GetProcessesByName(proc)
                    For Each pr As Process In procs
                        pr.Kill()
                    Next
                End If
            Next

        Catch ex As Exception
            Log.E(LOGNAME & "Error in command Restore")
        End Try
    End Sub
#End Region

End Class
