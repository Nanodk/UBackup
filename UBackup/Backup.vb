Imports System.Collections.Concurrent
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading

''' <summary>
''' Prepare files and upload to newsgroup
''' </summary>
Public Class Backup

#Region "Constantes"
    Const LOGNAME As String = "[BACKUP] "
    Public Const TITLE As String = "BACKUP"
    Private Const MAX_UPLOAD_QUEUE As Integer = 5
    Private Const SLEEPER As Integer = 50
#End Region

#Region "Properties"
    Private Shared ThreadPrepare As Thread
    Private Shared ThreadUpload As Thread
    Private Shared IsStarted As Boolean
    Private shared IsUploading As Boolean
    Private Shared ListOfDboToBackup As New ConcurrentQueue(Of Database.DatabaseObj)
#End Region

#Region "Start / Stop"
    ''' <summary>
    ''' Start workers
    ''' </summary>
    Public Shared Sub Start()
        Try
            IsStarted = True
            IsUploading = True
            Stats.NbFilesTotal = Database.FilesToProcess.Count
            Stats.TotalSize = (From x In Database.FilesToProcess Where x.Size.HasValue Select x.Size.Value).Sum
            Stats.DateStart = Date.UtcNow

            ThreadPrepare = New Thread(AddressOf ThreadPrepare_Work) With {.Name = "ThreadPrepare"}
            Call ThreadPrepare.Start()
            ThreadUpload = New Thread(AddressOf ThreadUpload_Work) With {.Name = "ThreadUpload"}
            Call ThreadUpload.Start()
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

#Region "Prepare"
    ''' <summary>
    ''' Prepare files to be upload (encryption and parity)
    ''' </summary>
    Private Shared Sub ThreadPrepare_Work()
        Log.I(LOGNAME & "Prepare Thread running")
        While IsStarted
            Try
                Utilities.UpdateTitle(Stats.Display(TITLE))
                If Database.FilesToProcess.Count  = 0 Then
                    Exit While
                End If
                If ListOfDboToBackup.Count >= MAX_UPLOAD_QUEUE Then
                    Thread.Sleep(SLEEPER)
                    Continue While
                End If
                Dim dbo As Database.DatabaseObj = Nothing
                If Database.FilesToProcess.TryDequeue(dbo) = False Then
                    Thread.Sleep(SLEEPER)
                    Continue While
                End If

                'CheckSum
                dbo.TempChecksum = Utilities.Checksum(New FileInfo(dbo.TempFileOrFolderPath))

                'Encryption
                Dim tmpGuid As String = Utilities.GenerateId & Config.UploadFileSuffix
                Dim folderPrepare As String = Path.Combine(Utilities.FolderTemp, tmpGuid)
                Call Utilities.EnsureDirectory(folderPrepare)

                Dim filePathSrc As String = dbo.TempFileOrFolderPath
                Dim filePathDest As String = Path.Combine(folderPrepare, tmpGuid)

                'Split And Xor
                If Utilities.SplitAndXor(Config.EncryptionKey, filePathSrc, filePathDest) = False Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing SplitAndXor """ & filePathSrc & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Continue While
                End If

                'Par2
                If ProcessWrapper.Par2Create(Utilities.FolderTemp, filePathDest) = False Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing PAR2 Create """ & filePathSrc & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Continue While
                End If

                'Add to ListOfDboToUpload
                dbo.TempGuid = tmpGuid
                Call ListOfDboToBackup.Enqueue(dbo)
            Catch ex As Exception
                Log.E(LOGNAME & ex.Message)
            End Try
        End While
        IsUploading = False
        Utilities.UpdateTitle(Stats.Display(TITLE))
    End Sub
#End Region

#Region "Upload"
    'Upload file to newsgroup and update db
    Private Shared Sub ThreadUpload_Work()
        Log.I(LOGNAME & "Upload Thread running")
        While IsStarted
            Try
                If ListOfDboToBackup.Count = 0 AndAlso IsUploading = False Then
                    Log.I(LOGNAME & "Upload Thread finished all jobs")
                    IsStarted = False
                    Exit While
                End If
                Dim dbo As Database.DatabaseObj = Nothing
                If ListOfDboToBackup.TryDequeue(dbo) = False Then
                    Thread.Sleep(SLEEPER)
                    Continue While
                End If

                Dim tmpGuid As String = dbo.TempGuid
                Dim folderUpload As String = Path.Combine(Utilities.FolderTemp, tmpGuid)

                Dim nzb As String = tmpGuid & ".nzb"
                Log.I(LOGNAME & "[UP] " & dbo.TempFileOrFolderPath)

                'NYUU
                Dim nzbFullPath As String = Path.Combine(Utilities.FolderTemp, nzb)
                If ProcessWrapper.NyuuUpload(Utilities.FolderTemp, nzb, folderUpload) = False Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing NYYU """ & dbo.TempFileOrFolderPath & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Continue While
                End If

                'We move nzbfile
                If File.Exists(nzbFullPath) = False OrElse New FileInfo(nzbFullPath).Length = 0 Then
                    Stats.NbFilesFail += 1
                    Log.E(LOGNAME & "Error processing NYYU """ & dbo.TempFileOrFolderPath & """ (" & tmpGuid & ")")
                    Call Utilities.DeleteAllFilesForGuid(tmpGuid)
                    Continue While
                End If
                Dim nzbDest As String = Path.Combine(Utilities.FolderNzb, nzb.Substring(0, 1), nzb)
                Call Utilities.DeleteFile(nzbDest)
                File.Move(nzbFullPath, nzbDest)

                'We remove all temp files
                Call Utilities.DeleteAllFilesForGuid(tmpGuid)

                'We save database
                dbo.NzbGuid = dbo.TempGuid
                dbo.NzbDate = Date.UtcNow
                dbo.Checksum = dbo.TempChecksum

                'stats
                Stats.NbFilesSuccess += 1
                Stats.TotalSizeProcessed += dbo.Size.Value

                Call Database.Save(True)
                Log.I(LOGNAME & "[OK] " & dbo.TempFileOrFolderPath & " (" & tmpGuid & ")")
            Catch ex As Exception
                Log.E(LOGNAME & ex.Message)
            End Try
        End While
    End Sub


#End Region

End Class
