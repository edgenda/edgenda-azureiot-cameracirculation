# Edgenda.AzureIoT.CameraCirculation.ConsoleApp

Console application which allows the fetch of traffic camera data fron Ville de Montréal Open Data Portal.

## Context

For the Azure IoT Edge course, this console application allows the capture of live traffic camera images. The images are provided by the [Ville de Montréal Open Data Portal Street Observation Cameras](http://donnees.ville.montreal.qc.ca/dataset/cameras-observation-routiere).

### How to use

Clone the project and build the Edgenda.AzureIoT.CameraCirculation.ConsoleApp project. Launch the application using:

```bash
$ dotnet Edgenda.AzureIoT.CameraCirculation.ConsoleApp.dll
starting
running
```

The default settings are:

- hostname: localhost

    Environment variable bound: **CAMERASERVER_ZMQ_SERVERHOSTNAME**

- basePort: 15000

    Environment variable bound: **CAMERASERVER_ZMQ_SERVERBASEPORT**

## Application Architecture

The application fetches the data from: [http://ville.montreal.qc.ca/circulation/sites/ville.montreal.qc.ca.circulation/files/cameras-de-circulation.json](http://ville.montreal.qc.ca/circulation/sites/ville.montreal.qc.ca.circulation/files/cameras-de-circulation.json)

Data is cached therefore multiple calls won't be throttled by the provider.

Two 0MQ (ZeroMQ) sockets are available:

- Port: basePort (15000)

    Request/Response socket, sample payload:

    ```json
    {
        "Name":"get-by-coordinates",
        "Parameters":[-73.532344350311,45.600982799511]
    }
    ```

    Sample client code:

    ```C#
        using NetMQ;
        using NetMQ.Sockets;
        using Newtonsoft.Json;
        using System;

        using (var testClientSocket = new RequestSocket($"tcp://{hostname}:{basePort}"))
        {
            testClientSocket.SendFrame("{\"Name\":\"get-by-coordinates\",\"Parameters\":-73.532344350311,45.600982799511]}");
            var response = testClientSocket.ReceiveFrameString();
        }
    ```

    **Coordinates in Parameters are Longitude/Latitude.**

    Returns the five (5) closest camera data:

    ```json
    [
        {
            "nid":241,
            "url":"http://ville.montreal.qc.ca/circulation/map/9/241",
            "titre":"Rue Saint-Donat et rue Sherbrooke",
            "id-camera":4,
            "id-arrondissement":9,
            "description":null,
            "axe-routier-nord-sud":"Rue Saint-Donat",
            "axe-routier-est-ouest":"Rue Sherbrooke",
            "url-image-en-direct":"http://www1.ville.montreal.qc.ca/Circulation-Cameras/GEN4.jpeg",
            "url-video-en-direct":"http://ville.montreal.qc.ca/circulation/GEN4.mp4"
        }
        ...
    ]
    ```

- Port: basePort + 1 (15001)

    Unused at the moment.
