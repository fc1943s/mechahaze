import { MouseEvent } from 'react';
import {
    SelectingState,
    State,
    Action,
    InputType,
    ActionEvent,
    DragCanvasState
} from '@projectstorm/react-canvas-core';
import {PortModel, DiagramEngine, DragDiagramItemsState, LinkModel} from '@projectstorm/react-diagrams-core';


// @ts-ignore
// noinspection TypeScriptPreferShortImport
import { CreateLinkState } from './CreateLinkState.ts';







export class DefaultState extends State<DiagramEngine> {
    dragCanvas: DragCanvasState;
    createLink: CreateLinkState;
    dragItems: DragDiagramItemsState;
    cb;

    constructor(cb) {
        super({ name: 'starting-state' });
        this.cb = cb;
        this.childStates = [new SelectingState()];
        this.dragCanvas = new DragCanvasState();
        this.createLink = new CreateLinkState(cb);
        this.dragItems = new DragDiagramItemsState();

        // determine what was clicked on
        this.registerAction(
            new Action({
                type: InputType.MOUSE_DOWN,
                fire: (event: ActionEvent<MouseEvent>) => {
                    const element = this.engine.getActionEventBus().getModelForEvent(event);

                    // the canvas was clicked on, transition to the dragging canvas state
                    if (!element) {
                        this.transitionWithEvent(this.dragCanvas, event);
                    }
                    // initiate dragging a new link
                    else if (element instanceof PortModel) {
                        return;
                    }
                    // move the items (and potentially link points)
                    else {
                        this.transitionWithEvent(this.dragItems, event);
                    }
                }
            })
        );

        this.registerAction(
            new Action({
                type: InputType.MOUSE_UP,
                fire: (event: ActionEvent<MouseEvent>) => {
                    const element = this.engine.getActionEventBus().getModelForEvent(event);

                    if (element instanceof PortModel) this.transitionWithEvent(this.createLink, event);
                }
            })
        );
    }
}
