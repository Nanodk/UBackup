
Imports System.Collections.Concurrent
Imports System.IO
Imports System.Threading

''' <summary>
''' Restore files from newsgroup
''' </summary>
Public Class Restore

#Region "Constantes"
    Const LOGNAME As String = "[RESTORE] "
    Public Const TITLE As String = "RESTORE"
    Private Const MAX_DOWNLOAD_QUEUE As Integer = 5
    Private Const SLEEPER As Integer = 50
#End Region

#Region "Properties"
    Private Shared ThreadDownload As Thread
    Private Shared ThreadRestore As Thread
    Private Shared IsStarted As Boolean
    Private Shared IsDownloading As Boolean
    Private Shared ListOfDboToRestore As New ConcurrentQueue(Of Database.DatabaseObj)
#End Region

#Region "Start / Stop"
    ''' <summary>
    ''' Start workers
    ''' </summary>
    Public Shared Sub Start()
        Try
            IsStarted = True
            IsDownloading = True
            Stats.NbFilesTotal = Database.FilesToProcess.Count
            Stats.TotalSize = (From x In Database.FilesToProcess Where x.Size.HasValue Select x.Size.Value).Sum
            Stats.DateStart = Date.UtcNow

            ThreadDownload = New Thread(AddressOf ThreadDownload_Work) With {.Name = "ThreadDownload"}
            Call ThreadDownload.Start()
            ThreadRestore = New Thread(AddressOf ThreadRestore_Work) With {.Name = "ThreadRestore"}
            Call ThreadRestore.Start()
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Stop workers
    ''' </summary>
    Public Shared Sub [Stop]()
        Try
            IsStarted = False
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
    End Sub
#End Region

#Region "Download"
    ''' <summary>
    ''' Download nzb file
    ''' </summary>
    Private Shared Sub ThreadDownload_Work()
        Log.I(LOGNAME & "Download Thread running")
        While IsStarted
            Try
                Utilities.UpdateTitle(Stats.Display(TITLE))

                If Database.FilesToProcess.Count = 0 Then
                    Exit While
                End If

                If ListOfDboToRestore.Count >= MAX_DOWNLOAD_QUEUE Then
                    Thread.Sleep(SLEEPER)
                    Continue While
                End If

                Dim dbo As Database.DatabaseObj = Nothing
                If Database.FilesToProcess.TryDequeue(dbo) = False Then
                    Thread.Sleep(SLEEPER)
                    Continue While
                End If

                If dbo.IsDirectory Then
                    ListOfDboToRestore.Enqueue(dbo)
                    Continue While
                End If

                Dim tmpGuid As String = dbo.NzbGuid
                Dim nzbPath As String = Path.Combine(Utilities.FolderTemp, tmpGuid & ".nzb")
                File.Copy(Path.Combine(Utilities.FolderNzb, tmpGuid.Substring(0, 1), tmpGuid & ".nzb"), nzbPath)

                'Download
                If ProcessWrapper.NzbGet(Utilities.FolderTemp, nzbPath) = False Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing NzbGet """ & nzbPath & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Continue While
                End If

                Call Utilities.DeleteFile(nzbPath)
                Dim folderPrepare As String = Path.Combine(Utilities.FolderTemp, tmpGuid)
                If Directory.Exists(folderPrepare) = False Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error checking files (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Continue While
                End If

                ''Move Files
                'Dim files As FileInfo() = (New DirectoryInfo(Utilities.FolderTemp)).GetFiles(tmpGuid & "*").ToArray
                'If files.Count = 0 OrElse (From x In files Where x.Name = tmpGuid & "." & Utilities.SPLIT_JOIN_PREFIX & "00").Count() <> 2 Then 'x.Name = tmpGuid & ".par2" Or
                '    Stats.NbFilesFail += 1
                '    Log.E(LOGNAME & "Error checking files (" & tmpGuid & ")")
                '    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                '    Continue While
                'End If
                'Call Utilities.EnsureDirectory(folderPrepare)
                'For Each fi As FileInfo In files
                '    fi.MoveTo(folderPrepare)
                'Next

                ListOfDboToRestore.Enqueue(dbo)

            Catch ex As Exception
                Log.E(LOGNAME & ex.Message)
            End Try
        End While
        IsDownloading = False
        Utilities.UpdateTitle(Stats.Display(TITLE))
    End Sub
#End Region

#Region "Restore"
    ''' <summary>
    ''' Restore original file
    ''' </summary>
    Private Shared Sub ThreadRestore_Work()
        Log.I(LOGNAME & "Restore Thread running")
        While IsStarted
            Try
                If ListOfDboToRestore.Count = 0 AndAlso IsDownloading = False Then
                    Log.I(LOGNAME & "Restore Thread finished all jobs")
                    IsStarted = False
                    Exit While
                End If

                Dim dbo As Database.DatabaseObj = Nothing
                If ListOfDboToRestore.TryDequeue(dbo) = False Then
                    Thread.Sleep(SLEEPER)
                    Continue While
                End If

                Dim dboPath As String = vbNullString
                If String.IsNullOrEmpty(dbo.TempFileOrFolderPath) Then
                    dboPath = Path.Combine(Utilities.FolderRestore, dbo.Name)
                Else 
                    dboPath = Path.Combine(Utilities.FolderRestore, dbo.TempFileOrFolderPath, dbo.Name)
                End If

                If dbo.IsDirectory Then
                    Call Utilities.EnsureDirectory(dboPath)
                    Continue While
                End If
                Log.I(LOGNAME & "[DL] " & Path.Combine(dbo.TempFileOrFolderPath, dbo.Name))
                Dim tmpGuid As String = dbo.NzbGuid
                Dim folderPrepare As String = Path.Combine(Utilities.FolderTemp, tmpGuid)

                ''Par2 'Commented because nzbget already check parity
                'Dim par2file As String = Path.Combine(folderPrepare, tmpGuid & ".par2")
                'If File.Exists(par2file) = False Then
                '    Log.W(LOGNAME & "PAR2 file not found """ & par2file & """ (" & tmpGuid & ")")
                'Else
                '    If ProcessWrapper.Par2Repair(Utilities.FolderTemp, par2file) = False Then
                '        Log.W(LOGNAME & "Error processing PAR2 Repair """ & par2file & """ (" & tmpGuid & ")")
                '    End If
                'End If

                'Join and Xor
                Dim files As FileInfo() = (New DirectoryInfo(folderPrepare)).GetFiles()
                If Utilities.JoinAndXor(Config.EncryptionKey, files, dboPath) = False Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing JoinAndXor """ & dboPath & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Call Utilities.DeleteFile(dboPath)
                    Continue While
                End If

                'CheckSum
                Dim checksum As String = Utilities.Checksum(New FileInfo(dboPath))
                If dbo.Checksum <> checksum Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing Checksum """ & dboPath & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Call Utilities.DeleteFile(dboPath)
                    Continue While
                End If

                Stats.NbFilesSuccess += 1
                Stats.TotalSizeProcessed += dbo.Size.Value
                Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                Log.I(LOGNAME & "[OK] " & dboPath & " (" & tmpGuid & ")")

            Catch ex As Exception
                Log.E(LOGNAME & ex.Message)
            End Try
        End While
    End Sub
#End Region
End Class
