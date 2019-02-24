Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' Some utilities functions
''' </summary>
Public Class Utilities

#Region "Constantes"
    Const LOGNAME As String = "[UTILITIES] "
#End Region

#Region "Properties"
    ''' <summary>
    ''' Default encoding to use
    ''' </summary>
    Public Shared UTF8 As Encoding = Encoding.UTF8

    ''' <summary>
    ''' Returns ExecutableFolder Path
    ''' </summary>
    Public Shared ExecutableFolder As String = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly.Location).ToString

    ''' <summary>
    ''' Returns temp folder
    ''' </summary>
    Public Shared FolderTemp As String = Path.Combine(Utilities.ExecutableFolder, "Temp")

    ''' <summary>
    ''' Return folder to store Nzb and database
    ''' </summary>
    Public Shared FolderNzb As String = Path.Combine(Utilities.ExecutableFolder, "Nzb")

    ''' <summary>
    ''' Return folder to store zip archives of nzb folder
    ''' </summary>
    Public Shared FolderZip As String = Path.Combine(Utilities.ExecutableFolder, "Zip")

    ''' <summary>
    ''' Return folder to save restored data
    ''' </summary>
    Public Shared FolderRestore As String = Path.Combine(Utilities.ExecutableFolder, "Restore")

    ''' <summary>
    ''' Returns Assembly Name
    ''' </summary>
    Public Shared AssemblyName As String = System.Reflection.Assembly.GetExecutingAssembly.GetName().Name

    ''' <summary>
    ''' Random Object
    ''' </summary>
    Public Shared Rnd As New Random(Environment.TickCount)

#End Region

#Region "Checksum"
    ''' <summary>
    ''' Calculate a byte array checksum
    ''' </summary>
    Public Shared Function Checksum(ByRef buf As Byte()) As String
        Dim ret As String = vbNullString
        Try
            Using sha As New SHA256Managed
                Dim hash() As Byte = sha.ComputeHash(buf)
                ret = BitConverter.ToString(hash).Replace("-", "")
            End Using
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return ret
    End Function

    ''' <summary>
    ''' Calculate a file checksum (from FileInfo) 
    ''' </summary>
    Public Shared Function Checksum(fi As FileInfo) As String
        Dim ret As String = vbNullString
        Try
            Using fs As New FileStream(fi.FullName, FileMode.Open, FileAccess.Read)
                Using sha As New SHA256Managed
                    Dim hash() As Byte = sha.ComputeHash(fs)
                    ret = BitConverter.ToString(hash).Replace("-", "")
                End Using
            End Using
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return ret
    End Function
#End Region

#Region "Read And XOR Functions"
    ''' <summary>
    ''' Generate FileBucketParts with split and xor encryption
    ''' </summary>
    Public Shared Function Xorify(filePathSrc As String, filePathDest As String, ByRef encryptionPassphrase As String) As Boolean
        Const BUFFER_SIZE As Integer = 32 * 1024 * 1024
        Try
            Dim encryptionKey() As Byte = UTF8.GetBytes(Checksum(UTF8.GetBytes(encryptionPassphrase)))

            Try
                If String.IsNullOrEmpty(filePathDest) = False Then
                    Call Utilities.DeleteFile(filePathDest)
                End If
            Catch ex As Exception
                Log.E(LOGNAME & ex.Message)
            End Try

            Using bw As New BinaryWriter(New FileStream(filePathDest, FileMode.CreateNew))
                Using fs As New FileStream(filePathSrc, FileMode.Open, FileAccess.Read)
                    fs.Seek(0, SeekOrigin.Begin)
                    Dim buf(BUFFER_SIZE - 1) As Byte
                    Dim nbRead As Integer = fs.Read(buf, 0, BUFFER_SIZE)
                    While nbRead > 0
                        'XOR Encryption
                        If Xorify(buf, encryptionKey) = False Then
                            Return False
                        End If
                        If nbRead <> BUFFER_SIZE Then
                            buf = buf.Take(nbRead).ToArray
                        End If

                        'Write File in FolderTemp
                        bw.Write(buf)
                        nbRead = fs.Read(buf, 0, buf.Length)
                    End While
                End Using
            End Using

            Return True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Encrypt Data
    ''' </summary>
    Private Shared Function Xorify(ByRef buf As Byte(), encryptionKey As Byte()) As Boolean
        Dim ret As Boolean = False
        Try
            Dim j As Integer = 0
            Dim encLen As Integer = encryptionKey.Length
            For i As Integer = 0 To buf.Length - 1
                buf(i) = buf(i) Xor encryptionKey(j)
                j += 1
                If j = encLen Then
                    j = 0
                End If
            Next
            ret = True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return ret
    End Function
#End Region

#Region "Split / Join"
    ''' <summary>
    ''' Prefix for parts
    ''' </summary>
    Public Const SPLIT_JOIN_PREFIX As String = "z"

    ''' <summary>
    ''' Return the most appropriate split file size
    ''' </summary>
    ''' <param name="fi"></param>
    Private Shared Function SplitJoinGetSize(fi As FileInfo) As Integer
        Const SIZE_DEFAULT As Integer = 50 * 1024 * 700
        Const VERYBIG As Integer = 2000 * 1024 * 1024
        Const BIG As Integer = 700 * 1024 * 1024
        Try
            If fi.Length > VERYBIG Then
                Return 300 * 1024 * Config.UploadArticleSize
            ElseIf fi.Length > BIG Then
                Return 150 * 1024 * Config.UploadArticleSize
            Else
                Return 50 * 1024 * Config.UploadArticleSize
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return SIZE_DEFAULT
    End Function

    ''' <summary>
    ''' Generate parts with split and xor encryption
    ''' </summary>
    Public Shared Function SplitAndXor(encryptionKey() As Byte, filePathSrc As String, filePathDest As String) As Boolean
        Try
            Dim fi As New FileInfo(filePathSrc)
            Dim BUFFER_SIZE As Integer = SplitJoinGetSize(fi)

            Dim n As Integer = 0
            Using fs As New FileStream(filePathSrc, FileMode.Open, FileAccess.Read)
                fs.Seek(0, SeekOrigin.Begin)
                Dim buf(BUFFER_SIZE - 1) As Byte
                Dim nbRead As Integer = fs.Read(buf, 0, BUFFER_SIZE)
                While nbRead > 0
                    If nbRead <> BUFFER_SIZE Then
                        buf = buf.Take(nbRead).ToArray
                    End If

                    'Xorify
                    If Xorify(buf, encryptionKey) = False Then
                        Return False
                    End If

                    'Write File in FolderTemp
                    Using bw As New BinaryWriter(New FileStream(filePathDest & "." & SPLIT_JOIN_PREFIX & n.ToString("00"), FileMode.Create))
                        bw.Write(buf)
                    End Using
                    n += 1
                    nbRead = fs.Read(buf, 0, buf.Length)
                End While
            End Using

            Return True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Join parts into single file and xor encryption
    ''' </summary>
    Public Shared Function JoinAndXor(encryptionKey() As Byte, files As FileInfo(), filePathDest As String) As Boolean
        Try
            files = (From x In files Where String.IsNullOrEmpty(x.Extension) = False AndAlso x.Extension.StartsWith("." & SPLIT_JOIN_PREFIX) Order By x.Name Ascending).ToArray
            Using bw As New BinaryWriter(New FileStream(filePathDest, FileMode.Create))
                For Each fi As FileInfo In files
                    Dim buf() As Byte = File.ReadAllBytes(fi.FullName)
                    If Xorify(buf, encryptionKey) = False Then
                        Return False
                    End If
                    bw.Write(buf)
                Next
            End Using
            Return True
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return False
    End Function

#End Region

#Region "Converters"
    ''' <summary>
    ''' Convert a byte size in human readable format
    ''' </summary>
    Public Shared Function ConvertSizeToHumanReadable(tmpSize As Double) As String
        Dim ret As String = vbNullString
        Try
            Dim sizes As String() = {"", "K", "M", "G", "T"}
            Dim order As Integer = 0
            While tmpSize >= 1024 AndAlso order < sizes.Length - 1
                order += 1
                tmpSize = tmpSize / 1024
            End While
            ret = String.Format("{0:0.##}{1}", CLng(tmpSize), sizes(order))
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return ret
    End Function
#End Region

#Region "Helpers"
    ''' <summary>
    ''' Generate an id
    ''' </summary>
    Public Shared Function GenerateId() As String
        Return Guid.NewGuid().ToString().Replace("-", "")
    End Function

    ''' <summary>
    ''' Update the display console title
    ''' </summary>
    Public Shared Sub UpdateTitle(msg As String)
        Dim tmp As String() = Split(Console.Title, " - ")
        Console.Title = tmp(0) & " - " & msg
    End Sub

    ''' <summary>
    ''' Generate a random poster
    ''' </summary>
    Public Shared Function RandomPoster() As String
        Dim sizeUser As Integer = Rnd.Next(5, 10)
        Dim sizeEmail As Integer = Rnd.Next(5, 10)
        Const ALPHABET As String = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim user As String = ""
        Dim email As String = ""
        For i As Integer = 0 To sizeUser
            user &= ALPHABET(Rnd.Next(0, ALPHABET.Length - 1))
        Next
        For i As Integer = 0 To sizeEmail
            email &= ALPHABET(Rnd.Next(0, ALPHABET.Length - 1))
        Next
        Return user & " <" & user & "@" & email & ".com>"
    End Function

    ''' <summary>
    ''' Try to delete a file
    ''' </summary>
    Public Shared Sub DeleteFile(filePath As String)
        Try
            If File.Exists(filePath) Then
                File.Delete(filePath)
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Try to delete a directory
    ''' </summary>
    Public Shared Sub DeleteDirectory(dirPath As String)
        Try
            If Directory.Exists(dirPath) Then
                Directory.Delete(dirPath, True)
            End If
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Delete all temporary files for a guid
    ''' </summary>
    Public Shared Sub DeleteAllFilesForGuid(tmpGuid As String)
        Try
            Dim dir As String = Path.Combine(Utilities.FolderTemp, tmpGuid)
            Utilities.DeleteDirectory(dir)
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Ensure a directory exists
    ''' </summary>
    ''' <param name="dirPath"></param>
    Public Shared Function EnsureDirectory(dirPath As String) As Boolean
        Dim ret As Boolean = True
        Try
            If Directory.Exists(dirPath) = False Then
                Directory.CreateDirectory(dirPath)
            End If
        Catch ex As Exception
            ret = False
        End Try
        Return ret
    End Function
#End Region
End Class
