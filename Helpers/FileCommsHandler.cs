using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
  public class FileCommsHandler
  {
    private readonly ConversionHandler _conversionHandler;
    private readonly FileHandler _fileHandler;
    private readonly FileStreamHandler _fileStreamHandler;
    private readonly Connections _socketHelper;

    public FileCommsHandler()
    {
      _conversionHandler = new ConversionHandler();
      _fileHandler = new FileHandler();
      _fileStreamHandler = new FileStreamHandler();
    }

    public void DeleteImage(string fileName)
    {
      string folderPath = @"C:\FotosPr";
      string filePath = Path.Combine(folderPath, fileName);
      if (File.Exists(filePath))
      {
        File.Delete(filePath);
      }
    }


    public async Task SendFile(string path, TcpClient clientSocket)
    {
      if (_fileHandler.FileExists(path))
      {
        var fileName = _fileHandler.GetFileName(path);
        await Connections.Send(_conversionHandler.ConvertIntToBytes(fileName.Length), clientSocket);
        await Connections.Send(_conversionHandler.ConvertStringToBytes(fileName), clientSocket);
        long fileSize = _fileHandler.GetFileSize(path);
        var convertedFileSize = _conversionHandler.ConvertLongToBytes(fileSize);
        await Connections.Send(convertedFileSize, clientSocket);
        await SendFileWithStream(fileSize, path, clientSocket);
      }
      else
      {
        await Connections.Send(_conversionHandler.ConvertIntToBytes(-1), clientSocket);
        throw new Exception("File does not exist");
      }
    }

    public async Task<string> ReceiveFile(TcpClient clientSocket, string fileId)
    {
      int fileNameSize = _conversionHandler.ConvertBytesToInt( await
          Connections.Receive(Protocol.FixedDataSize, clientSocket));
      if (fileNameSize == -1)
      {
        throw new Exception("File does not exist, cancelled operation");
      }
      string fileName = _conversionHandler.ConvertBytesToString(await Connections.Receive(fileNameSize, clientSocket));
      string storedFileName = fileId == "-1" ? $"Cliente{fileName}" : $"{fileId}.{fileName.Substring(fileName.LastIndexOf('.') + 1)}";
      long fileSize = _conversionHandler.ConvertBytesToLong(await
          Connections.Receive(Protocol.FixedFileSize, clientSocket));
      await ReceiveFileWithStreams(fileSize, storedFileName, clientSocket);
      return storedFileName;
    }

    private async Task SendFileWithStream(long fileSize, string path, TcpClient clientSocket)
    {
      long fileParts = Protocol.CalculateFileParts(fileSize);
      long offset = 0;
      long currentPart = 1;

      while (fileSize > offset)
      {
        byte[] data;
        if (currentPart == fileParts)
        {
          var lastPartSize = (int)(fileSize - offset);
          data = _fileStreamHandler.Read(path, offset, lastPartSize);
          offset += lastPartSize;
        }
        else
        {
          data = _fileStreamHandler.Read(path, offset, Protocol.MaxPacketSize);
          offset += Protocol.MaxPacketSize;
        }

        await Connections.Send(data, clientSocket);
        currentPart++;
      }
    }

    private async Task ReceiveFileWithStreams(long fileSize, string fileId, TcpClient clientSocket)
    {
      long fileParts = Protocol.CalculateFileParts(fileSize);
      long offset = 0;
      long currentPart = 1;
      string folderPath = @"C:\FotosPr"; // Ruta donde se guardarán los archivos

      // Si la carpeta no existe, la creamos
      if (!Directory.Exists(folderPath))
      {
        Directory.CreateDirectory(folderPath);
      }

      string filePath = Path.Combine(folderPath, fileId); // Ruta completa del archivo

      if (File.Exists(filePath))
      {
        File.Delete(filePath);
      }

      using (FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
      {
        while (fileSize > offset)
        {
          byte[] data;
          if (currentPart == fileParts)
          {
            var lastPartSize = (int)(fileSize - offset);
            data = await Connections.Receive(lastPartSize, clientSocket);
            offset += lastPartSize;
          }
          else
          {
            data = await Connections.Receive(Protocol.MaxPacketSize, clientSocket);
            offset += Protocol.MaxPacketSize;
          }
          fileStream.Write(data, 0, data.Length);
          currentPart++;
        }
      }
    }
  }
}

