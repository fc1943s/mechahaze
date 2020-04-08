namespace MechaHaze.UI.Frontend

open Fable.React
open Fable.React.Props
open Fulma

module TabManager =

    type State<'T> =
        { ActiveTab: 'T }

    type Message<'T> =
        | SetTab of 'T
        
    type Manager<'T> =
        { //State: IReducerHook<State<'T>, Message<'T>>
          IsActive: 'T -> Tabs.Tab.Option
          CreateOnClick: 'T -> DOMAttr
          SetTab: 'T -> unit
          CreateDisplay: 'T -> CSSProp }
        
    let create<'T when 'T: equality> (defaultTab: 'T) =
        let state =
            Hooks.useReducer ((fun state dispatch ->
                match dispatch with
                | SetTab tab ->
                    { state with ActiveTab = tab }
            ), { ActiveTab = defaultTab })
            
        let isActive (tab: 'T) =
            Tabs.Tab.IsActive (state.current.ActiveTab = tab)
            
        let setTab (tab: 'T) =
            state.update (SetTab tab)
            
        let createOnClick (tab: 'T) =
            OnClick (fun _ -> setTab tab)
            
        let createDisplay (tab: 'T) =
            Display (if state.current.ActiveTab = tab then DisplayOptions.Block else DisplayOptions.None)
            
        { IsActive = isActive
          CreateOnClick = createOnClick
          SetTab = setTab
          CreateDisplay = createDisplay }

