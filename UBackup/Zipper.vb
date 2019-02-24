Imports System.IO
Imports System.IO.Packaging
Imports System.Text.RegularExpressions

''' <summary>
''' Zip a folder
''' </summary>
Public Class Zipper

#Region "Constantes"
    Const LOGNAME As String = "[ZIPPER] "
#End Region

    ''' <summary>
    ''' Create a zip archive for specified folder
    ''' </summary>
    Public Shared Function Zip(folderPath As String, backupFileName As String) As Boolean
        Try
            Log.I(LOGNAME & "Zipping Nzb folder")
            Dim dInfoParent As New DirectoryInfo(folderPath)
            Using pac As Packaging.Package = Packaging.Package.Open(backupFileName, FileMode.Create)
                ZipPackageForEachFolder(pac, dInfoParent, dInfoParent.Name)
            End Using
            Log.I(LOGNAME & "Zip finished and available in Nzb folder: " & backupFileName)
            Return True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Recursive function for package each folder
    ''' </summary>
    Private Shared Sub ZipPackageForEachFolder(pac As Packaging.Package, parentDirectoryInfo As DirectoryInfo, partName As String)
        Dim childFileInfo As FileInfo() = parentDirectoryInfo.GetFiles()
        For Each file As FileInfo In childFileInfo
            Dim fileUri As Uri = PackUriHelper.CreatePartUri(New Uri(Path.Combine(partName, file.Name), UriKind.Relative))
            'Add the Document part to the Package
            Dim packagePartDocument As PackagePart = pac.CreatePart(fileUri, Net.Mime.MediaTypeNames.Application.Octet, CompressionOption.Normal)
            'Copy the data to the Document Part
            Using fs As New FileStream(Path.Combine(parentDirectoryInfo.FullName, file.Name), FileMode.Open, FileAccess.Read)
                CopyStream(fs, packagePartDocument.GetStream())
            End Using
        Next

        Dim childDirectoryInfo As DirectoryInfo() = parentDirectoryInfo.GetDirectories()
        For Each dInfo As DirectoryInfo In childDirectoryInfo
            ZipPackageForEachFolder(pac, dInfo, Path.Combine(partName, dInfo.Name))
        Next
    End Sub

    ''' <summary>
    ''' Copy Stream
    ''' </summary>
    Private Shared Sub CopyStream(source As Stream, target As Stream)
        Const BUFFER_SIZE As Integer = 16 * 1024 * 1024
        Dim buf(BUFFER_SIZE - 1) As Byte
        Dim bytesRead As Integer = source.Read(buf, 0, BUFFER_SIZE)
        While bytesRead > 0
            target.Write(buf, 0, bytesRead)
            bytesRead = source.Read(buf, 0, BUFFER_SIZE)
        End While
    End Sub
End Class
