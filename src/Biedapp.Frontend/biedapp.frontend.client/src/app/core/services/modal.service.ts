import { ComponentRef, Injectable, TemplateRef } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export type Color = 'white' | 'red' | 'green' | 'yellow' | 'blue' | 'orange';

export interface ModalConfig {
  templateName: ModalTemplate;
  data?: any;
  submitButtonConfig?: ModalButtonConfig;
  resetButtonConfig?: ModalButtonConfig;
  closeButtonConfig?: ModalButtonConfig;
  title?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}

export interface ModalButtonConfig {
  show?: boolean;
  customText?: string;
  customColor?: string;
  customBackgroundColor?: Color;
}

export enum ModalTemplate {
  TRANSACTION_CREATE = 'TRANSACTION_CREATE',
  TRANSACTION_EDIT = 'TRANSACTION_EDIT',
  TRANSACTION_DELETE = 'TRANSACTION_DELETE'
}

export interface ModalResult {
  action: 'submit' | 'close' | 'reset';
  data?: any;
}

@Injectable({
  providedIn: 'root'
})
export class ModalService {
 private modalComponent: ComponentRef<any> | null = null;
 private resultSubject = new Subject<ModalResult>();

 open(config: ModalConfig): Observable<ModalResult> {
  this.resultSubject = new Subject<ModalResult>();

  if(this.modalComponent) {
    this.modalComponent.instance.openModal(config);
  }

  return this.resultSubject.asObservable();
 }

 close(result: ModalResult): void {
  this.resultSubject.next(result);
  this.resultSubject.complete();
 }

 registerModal(componentRef: ComponentRef<any>): void {
  this.modalComponent = componentRef;
 }

 unregisterModal(): void {
  this.modalComponent = null;
 }
}
