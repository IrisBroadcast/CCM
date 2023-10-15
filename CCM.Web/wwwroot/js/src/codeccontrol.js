
/* *******************************************************
 * Codec control for front page popup */
ccmControllers.controller('sipInfoController', function ($scope, $http, $interval, $uibModalInstance, registrationId, sipAddress, userAccountId) {

    $scope.codecControlHost = window.codecControlHost;
    $scope.userName = window.codecControlUserName;
    $scope.password = window.codecControlPassword;
    $scope.discoveryServiceUrl = window.discoveryServiceUrl;
    $scope.codecOnline = false;  // 'true' if codec is reachable
    $scope.registrationId = registrationId;
    $scope.userAccountId = userAccountId;
    $scope.sipAddress = sipAddress;
    $scope.info = {};
    $scope.streamInfo = {"lostPackets":0,"lostPacketsPerSec":0,"recoveredPackets":0,"recoveredPacketsPerSec":0,"obsoletePackets":0,"obsoletePacketsPerSec":0,"jitter":0,"jitterPerSec":0,"roundTrip":0,"roundTripPerSec":0,"txKbps":0,"rxKbps":0,"sipAddress":""};
    $scope.codecControlLoading = true;
    $scope.codecControlAvailable = false;
    $scope.codecMeteringErrorMessage = "";
    $scope.txL = -96;
    $scope.txR = -96;
    $scope.rxL = -96;
    $scope.rxR = -12;
    $scope.rebootconfirm1 = false;
    $scope.rebootconfirm2 = false;
    $scope.presetToLoad = '';
    $scope.selectedLine = '';
    $scope.selectedDestination = '';
    $scope.selectedProfile = '';
    $scope.callProfiles = [];
    $scope.gpos = [];
    $scope.inputs = [];
    $scope.lines = [];
    $scope.lineStatusArea = '';
    $scope.audioModeArea = '';
    $scope.signalrConnection = null;

    console.info('Getting codec information for ' + sipAddress);

    $scope.setCodecIsOnline = (codecIsOnline) => {
        // Codec is reachable
        console.info('Codec is online:', codecIsOnline);
        $scope.codecOnline = codecIsOnline;

        if (codecIsOnline) {
            $scope.getAvailableGpos();
            $scope.getAudioStatus();
            //$scope.startAudioStatus();
            $scope.startAudioUpdateHub();
        }
    };

    $scope.startSignalrConnection = () => {
        if (!$scope.signalrConnection) console.error('SignalrConnection must be configured before it can be started.');

        console.log('Initiating Signalr connection', $scope.signalrConnection);

        $scope.signalrConnection.start()
            .then((result) => {
                console.log("Signalr started");
                $scope.codecControlLoading = false;

                $scope.signalrConnection.invoke("Subscribe", $scope.sipAddress).catch((err) => {
                    $scope.codecMeteringErrorMessage = err;
                    return console.error(err.toString());
                });
            })
            .catch((err) => {
                setTimeout($scope.startSignalrConnection, 3000);
                return console.error(err.toString());
            });
    };

    $scope.startAudioUpdateHub = () => {
        $scope.codecControlLoading = true;

        $scope.signalrConnection = new signalR.HubConnectionBuilder()
            .withUrl($scope.codecControlHost + "/audioStatusHub")
            .configureLogging(signalR.LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        $scope.signalrConnection.on("AudioStatus", (sipAddress, audioStatus) => {
            //console.log("AudioStatus received", sipAddress, audioStatus);
            if (sipAddress === $scope.sipAddress) {
                $scope.updateAudioStatus(audioStatus);
            }
        });

        var sipJitterBufferArray = [];
        var graph = document.getElementById("diagram-poly");
        $scope.signalrConnection.on("StreamStatus", (sipAddress, streamStatus) => {
            if (sipAddress === $scope.sipAddress) {
                $scope.streamInfo = streamStatus;

                sipJitterBufferArray.push($scope.streamInfo.jitterPerSec);
                if (sipJitterBufferArray.length > 40) {
                    sipJitterBufferArray.splice(0, 1);
                }

                let flattened = "";
                for (var i = 0; i < sipJitterBufferArray.length; ++i) {
                    const current = (sipJitterBufferArray[i] / 1200) * -100;
                    flattened += i*10 + "," + current + " ";
                }

                window.requestAnimationFrame(function() {
                    graph.setAttributeNS(null, "points", flattened);
                });
            }
        });

        $scope.signalrConnection.on("Subscribed", (subscriptionInfo) => {
            console.log("Subscribed received", subscriptionInfo);
        });

        $scope.signalrConnection.on("Unubscribed", (subscriptionInfo) => {
            console.log("Unubscribed received", subscriptionInfo);
        });

        $scope.signalrConnection.onclose((e) => {
            console.log('Signalr connection closed');
            $scope.codecControlAvailable = false;
            $scope.codecControlLoading = false;
            $scope.codecMeteringErrorMessage = "";
            //setTimeout($scope.startSignalrConnection, 3000);
        });

        $scope.startSignalrConnection();
    }

    // API
    $scope.getAvailableGpos = () => {
        console.info('Getting GPOs');
        $http.get($scope.codecControlHost + '/api/codeccontrol/getavailablegpos?sipaddress=' + $scope.sipAddress)
            .then((response) => { // on success
                console.info('Available GPOs', response.data);
                $scope.gpos = response.data.gpos;
            },
            () => { // on error
                $scope.gpos = [];
            });
    };

    $scope.setGpo = (gpo) => {
        let newState = gpo.active ? false : true;
        $scope.httpPost('/api/codeccontrol/setgpo', { sipaddress: $scope.sipAddress, number: gpo.number, active: newState })
            .then((data) => {
                console.log("Codec GPO data: ", data);
                gpo.active = data.active;
            });
    };

    $scope.getLineStatus = (line) => {
        $http.get($scope.codecControlHost + '/api/codeccontrol/getlinestatus?sipaddress=' + $scope.sipAddress)
            .then((response) => {
                const data = response.data;
                console.log("Codec Line status data: ", data);
                let msg = "LineStatus: " + data.lineStatus + ". DisconnectReason: " + data.disconnectReasonDescription + " (" + data.disconnectReasonCode + ").\r\n";
                $scope.lineStatusArea = msg + $scope.lineStatusArea;
            });
    };

    $scope.getAudioMode = () => {
        $http.get($scope.codecControlHost + '/api/codeccontrol/GetAudioMode?sipaddress=' + $scope.sipAddress).then((response) => {
            const data = response.data;
            let msg;
            if (data.Error) {
                msg = data.Error;
            } else {
                msg = "AudioMode: Encoder=" + data.encoderAudioModeDescription + " (" + data.encoderAudioMode + ")" +
                    ", Decoder=" + data.decoderAudioModeDescription + " (" + data.decoderAudioMode + ")";
            }
            $scope.audioModeArea = msg + "\r\n" + $scope.audioModeArea;
        });
    };

    $scope.controlCall = () => {
        console.info(`Make call to '${$scope.selectedDestination}' with '${$scope.selectedProfile.name}'`);
        if ($scope.selectedDestination != '' && $scope.selectedProfile.name != '') {
            $scope.httpPost('/api/codeccontrol/call', {
                callee: $scope.selectedDestination,
                profileName: $scope.selectedProfile.name,
                deviceEncoder: null,
                sipAddress: $scope.sipAddress
            })
            .then((data) => {
                console.log("Call response data: ", data);
            });
        }
    };

    $scope.controlHangUp = () => {
        console.info('Hang up call');
        $scope.httpPost('/api/codeccontrol/hangup', {
            deviceEncoder: null,
            sipAddress: $scope.sipAddress
        })
        .then((data) => {
            console.log("Hangup response data: ", data);
        });
    };

    $scope.getCallProfiles = () => {
        $scope.httpGet('/discoveryviewaction/profiles').then((response) => {
            const data = response;
            console.log(data)
            $scope.callProfiles = data;
        },(err) => {
            console.error(err);
        });
    }

    $scope.getAudioStatus = () => {
        console.info('Getting codec audio status');

        $http.get($scope.codecControlHost + '/api/codeccontrol/getaudiostatus?sipaddress=' + $scope.sipAddress).then((response) => {
            console.info('Available audio status', response.data);
            const audioStatus = response.data;

            $scope.codecControlLoading = false;

            // Make initial parsing of codec information
            $scope.inputs = [];
            for (let i = 0; i < audioStatus.inputStatus.length; i++) {
                const input = {
                    id: i,
                    number: i + 1,
                    value: 0,
                    changeValue: 0,
                    enabled: false,
                    disabled: 'disabled'
                };
                $scope.inputs.push(input);
            }

            $scope.updateAudioStatus(audioStatus);
        }, (response) => {
            // Stop interval checking Audio status
            console.warn(response);
            $scope.codecControlAvailable = false;
            $scope.codecControlLoading = false;
            $scope.codecMeteringErrorMessage = response.data;
            //$scope.stopAudioStatus();
        });
    };

    $scope.updateAudioStatus = (audioStatus) => {
        
        for (let i = 0; i < audioStatus.inputStatus.length; i++) {
            $scope.setInputValue(audioStatus.inputStatus[i]);
        }

        $scope.codecControlAvailable = true;

        for (var j = 0; j < audioStatus.gpos.length; j++) {
            var gpoData = audioStatus.gpos[j];
            var gpo = $scope.gpos[gpoData.index];
            if (gpo) {
                gpo.active = gpoData.active;
            }
        }

        // Values is presented in dB where 0 = Fullscale +18db, -18 = Test 0dB, -96 = min-level
        $scope.txL = fallback($scope.txL, convertVuToPercentage(audioStatus.vuValues.txLeft));
        $scope.txR = fallback($scope.txR, convertVuToPercentage(audioStatus.vuValues.txRight));
        $scope.rxL = fallback($scope.rxL, convertVuToPercentage(audioStatus.vuValues.rxLeft));
        $scope.rxR = fallback($scope.rxR, convertVuToPercentage(audioStatus.vuValues.rxRight));

        // $scope.$apply(function () {

        // });
    };

    const fallback = (lastValue, newValue) => {
        const updateInterval = 500; // i ms
        const decayRate = 12.0; // i dB/s
        const maxDecay = decayRate * updateInterval / 1000;
        return newValue >= lastValue ? newValue : Math.max(newValue, lastValue - maxDecay);
    };

    function convertVuToPercentage(value) {
        // Konverterar db till utslag på mätare mellan 0% och 100%
        // Skalan är "linjär db" där varje db ger lika mycket utslag på mätaren.
        let maxScaleValue = 0; // dB-värde då mätaren ska visa 100%
        let minScaleValue = -60; // dB-värde då mätaren ska visa 0%
        let scaleLength = maxScaleValue - minScaleValue;

        value = value + scaleLength;
        let p = value / scaleLength * 100;
        return p > 100 ? 100 : (p < 0 ? 0 : p);
    }

    $scope.setInputValue = (inputStatus) => {
        let input = $scope.inputs[inputStatus.index];

        if (input) {
            if (inputStatus.error) {
                input.error = inputStatus.error;
                input.disabled = 'disabled';
            } else {
                input.value = inputStatus.gainLevel;
                input.enabled = inputStatus.enabled;
                input.disabled = '';
                if (input.interactingValue) {
                    input.changeValue = inputStatus.gainLevel;
                }
            }
        }

        $scope.$digest();

        // $scope.$apply(function () {
        //     if (input) {
        //         if (inputStatus.error) {
        //             input.error = inputStatus.error;
        //             input.disabled = 'disabled';
        //         } else {
        //             input.value = inputStatus.gainLevel;
        //             input.enabled = inputStatus.enabled;
        //             input.disabled = '';
        //             if (input.interactingValue) {
        //                 input.changeValue = inputStatus.gainLevel;
        //             }
        //         }
        //     }
        // });
    };

    $scope.toggleInputEnabled = (input) => {
        const enabled = input.enabled ? false : true;
        let data = { sipAddress: $scope.sipAddress, input: input.id, enabled: enabled };
        $scope.httpPost('/api/codeccontrol/setinputenabled', data).then((data) => {
            let isEnabled = data.enabled;
            console.log("SetInputEnabled", input.id, isEnabled);
            input.enabled = isEnabled;
        });
    };

    $scope.setGainLevel = (input, value) => {
        $scope.httpPost('/api/codeccontrol/setinputgain', { sipAddress: $scope.sipAddress, input: input.id, level: input.value + value })
            .then((data) => {
                input.value = data.gainLevel;
                input.changeValue = data.gainLevel;
            });
    };

    $scope.rangeSetInputTo = (input, value) => {
        console.log("Range input set to: " + value + ", for input: " + input.id);

        $scope.httpPost('/api/codeccontrol/setinputgain', { sipAddress: $scope.sipAddress, input: input.id, level: value })
            .then((data) => {
                input.value = data.gainLevel;
                input.changeValue = data.gainLevel;
            }, (error) => {
                console.error("Could not set input gain", error);
            });

        setTimeout(() => {
            input.interactingValue = false;
        }, 1000);
    };

    $scope.reboot = () => {
        this.rebootconfirm1 = false;
        this.rebootconfirm2 = false;
        $scope.httpPost('/api/codeccontrol/reboot', { sipaddress: $scope.sipAddress });
    };

    $scope.checkCodecAvailable = () => {
        console.info('Checking if codec is online');
        $http.get($scope.codecControlHost + '/api/codeccontrol/isavailable?sipaddress=' + $scope.sipAddress)
            .then((response) => {
                const data = response.data;
                console.debug('Codec available: ', data);
                let isAvailable = data.isAvailable;
                $scope.setCodecIsOnline(isAvailable);
            })
            .catch((err) => {
                console.error(err);
            });
    };

    // Modal codec information
    $scope.openAdmin = (link, width, height, scrollbars) => {
        if (width <= 0) {
            width = 1000;
        }

        if (height <= 0) {
            height = 620;
        }

        window.open(link, '_blank', 'width=' + width + ',height=' + height + ',status=no,menubar=no,resizable=no,scrollbars=' + (scrollbars ? 'yes' : 'no') + ',toolbar=no,location=no,directories=no');
        return false;
    };

    $scope.cancel = () => {
        console.log("Cancel called.");
        $uibModalInstance.dismiss('cancel');
    };

    $scope.$on("$destroy", () => {
        console.log("Destroy called for modal");
        $scope.codecControlLoading = false;

        // Stop subscription
        if ($scope.signalrConnection) {
            $scope.signalrConnection.invoke("Unsubscribe", null)
                .then(() => {
                    console.log("Unsubscribed");
                })
                .catch((err) => {
                    return console.error(err.toString());
                });

                $scope.signalrConnection.stop();
        }
    });

    let queryStr = `?id=${$scope.registrationId}`;
    if ($scope.userAccountId !== "" && $scope.userAccountId !== null) {
        queryStr = `?userId=${$scope.userAccountId}`;
    }

    $http.get(`/api/RegisteredCodec/ById/${queryStr}`).then((response) => {
        console.info('Codec information, Get registered User Agent Data', response.data);

        let info = response.data;
        $scope.info = info;

        // TODO: ADD THIS BACK WHEN AUTH CAN BE VERIFIED FUNCTIONING
        // if (info.codecControl) {
            // TODO: Here somethings should be automated... inputs and lines should not get from here. and what is lines?

            // Prepare available lines
            let lines = [];
            for (let j = 0; j < 4; j++) {
                const line = {
                    id: j,
                    number: j + 1
                };
                lines.push(line);
            }
            $scope.lines = lines;
            $scope.selectedLine = lines[0];

            $scope.checkCodecAvailable();
        // } else {
        //     console.warn('Codec-control is not Authorized');
        // }
    },
    (error) => {
        console.error("No answer from '/api/RegisteredCodec/ById/?id=" + $scope.registrationId + "'", error);
    });

    // Utilities
    $scope.httpPost = (apiPath, data) => {
        const authorizationBasic = window.btoa($scope.userName + ':' + $scope.password);
        const headers = { 'Authorization': 'Basic ' + authorizationBasic };

        return $http.post($scope.codecControlHost + apiPath, data, { headers: headers }).then(function (response) {
            return response.data;
        });
    };

    $scope.httpGet = (host) => {
        const authorizationBasic = window.btoa($scope.userName + ':' + $scope.password);
        const headers = { 'Authorization': 'Basic ' + authorizationBasic };

        return $http.get(host, { headers: headers }).then(function (response) {
            return response.data;
        });
    };
});
