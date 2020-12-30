namespace MechaHaze.UI.Frontend

open Feliz.Recoil
open MechaHaze.Shared.Bindings
open MechaHaze.Shared.SharedState
open MechaHaze.UI

[<AutoOpen>]
module State =


    module Atoms =
        let rec uiState =
            atom {
                key (nameof uiState)
                def UIState.State.Default
            }

        let rec debug =
            atom {
                key (nameof debug)
#if DEBUG
                def true
#else
                def false
#endif
            }

        let rec trackId =
            atom {
                key (nameof trackId)
                def Track.Default.Id
            }

        let rec autoLock =
            atom {
                key (nameof autoLock)
                def true
            }

        let rec recordingMode =
            atom {
                key (nameof recordingMode)
                def false
            }

        let rec activeBindingsPreset =
            atom {
                key (nameof activeBindingsPreset)
                def ""
            }

        let rec presetIdList =
            atom {
                key (nameof presetIdList)
                def ([]: PresetId list)
            }

        let rec processIdList =
            atom {
                key (nameof processIdList)
                def ([]: ProcessId list)
            }


    module AtomFamilies =
        module Track =
            let rec position =
                atomFamily {
                    key (nameof position)
                    def (fun (_id: TrackId) -> 0.)
                }

            let rec durationSeconds =
                atomFamily {
                    key (nameof durationSeconds)
                    def (fun (_id: TrackId) -> 0.)
                }

            let rec debugInfo =
                atomFamily {
                    key (nameof debugInfo)
                    def (fun (_id: TrackId) -> MatchDebugInfo.Default)
                }

            let rec locked =
                atomFamily {
                    key (nameof locked)
                    def (fun (_id: TrackId) -> false)
                }

            let rec offset =
                atomFamily {
                    key (nameof offset)
                    def (fun (_id: TrackId) -> 0.)
                }

            let rec timestamp =
                atomFamily {
                    key (nameof timestamp)
                    def (fun (_id: TrackId) -> 0L)
                }

        let rec timeSync =
            atomFamily {
                key (nameof timeSync)
                def (fun (_id: ProcessId) -> TimeSync.Default)
            }

        let rec preset =
            atomFamily {
                key (nameof preset)
                def (fun (_id: PresetId) -> Preset [])
            }

    module SelectorFamilies =
        let rec track =
            selectorFamily {
                key (nameof track)

                get (fun (trackId: TrackId) getter ->
                        {
                            Id = trackId
                            Position = getter.get (AtomFamilies.Track.position trackId)
                            DurationSeconds = getter.get (AtomFamilies.Track.durationSeconds trackId)
                            DebugInfo = getter.get (AtomFamilies.Track.debugInfo trackId)
                            Locked = getter.get (AtomFamilies.Track.locked trackId)
                            Offset = getter.get (AtomFamilies.Track.offset trackId)
                            Timestamp = getter.get (AtomFamilies.Track.timestamp trackId)
                        })
            }
