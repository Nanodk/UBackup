Imports System.IO
Imports System.Runtime.Serialization
Imports System.Xml.Serialization
Imports Newtonsoft.Json

''' <summary>
''' Object Serializer
''' </summary>
Public Class Serializer

#Region "Constantes"
    Const LOGNAME As String = "[SERIALIZER] "
#End Region


    ''' <summary>
    ''' Serialize an object
    ''' </summary>
    Public Shared Function Serialize(obj As Object, filePath As String) As Boolean
        Dim ret As Boolean = True
        Try
            Dim str As String = JsonConvert.SerializeObject(obj)
            Using sw As New StreamWriter(filePath, False)
                sw.Write(str)
            End Using
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
            ret = False
        End Try
        Return ret
    End Function

    ''' <summary>
    ''' Unserialize an object from file
    ''' </summary>
    Public Shared Function Deserialize(Of T)(filePath As String) As T
        Dim ret As T = Nothing
        Try
            Using sr As New StreamReader(filePath)
                Dim str As String = sr.ReadToEnd()
                ret = JsonConvert.DeserializeObject(Of T)(str)
            End Using
        Catch ex As Exception
            Log.E(LOGNAME & ex.Message)
        End Try
        Return ret
    End Function
End Class
