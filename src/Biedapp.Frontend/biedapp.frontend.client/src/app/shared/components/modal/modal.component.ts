import { CommonModule } from '@angular/common';
import { Component, ComponentRef, OnDestroy, OnInit, ViewChild, ViewContainerRef } from '@angular/core';
import { ModalConfig, ModalService, ModalTemplate } from '../../../core/services/modal.service';
import { TransactionModalCreateTemplateComponent } from './modal-templates/transactions/transaction-modal-create-template/transaction-modal-create-template.component';
import { TransactionModalDeleteTemplateComponent } from './modal-templates/transactions/transaction-modal-delete-template/transaction-modal-delete-template.component';
import { TransactionModalEditTemplateComponent } from './modal-templates/transactions/transaction-modal-edit-template/transaction-modal-edit-template.component';

@Component({
  selector: 'app-modal',
  standalone: true,
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss'],
  imports: [CommonModule]
})
export class ModalComponent implements OnInit, OnDestroy {
  @ViewChild('templateContainer', { read: ViewContainerRef }) templateContainer!: ViewContainerRef;
  
  isOpen = false;
  config: ModalConfig | null = null;
  templateComponentRef: ComponentRef<any> | null = null;

  constructor(private modalService: ModalService) {}

  ngOnInit(): void {
    this.modalService.registerModal({
      instance: this
    } as ComponentRef<any>);
  }

  ngOnDestroy(): void {
    this.modalService.unregisterModal();
    if(this.templateComponentRef) {
      this.templateComponentRef.destroy();
    }
  }

  openModal(config: ModalConfig): void {
    this.config = config;
    this.isOpen = true;
    setTimeout(() => this.loadTemplate(), 0);
  }

  private loadTemplate(): void {
    if(!this.config || !this.templateContainer) return;

    this.templateContainer.clear();
    if(this.templateComponentRef) {
      this.templateComponentRef.destroy();
    }

    const component = this.getTemplateComponent(this.config.templateName);
    if(component) {
      this.templateComponentRef = this.templateContainer.createComponent(component);

      if(this.config.data) {
        this.templateComponentRef.instance.data = this.config.data;
      }
    }
  }

  private getTemplateComponent(templateName: ModalTemplate): any {
    switch(templateName) {
      case ModalTemplate.TRANSACTION_CREATE:
        return TransactionModalCreateTemplateComponent;
      case ModalTemplate.TRANSACTION_EDIT:
        return TransactionModalEditTemplateComponent;
      case ModalTemplate.TRANSACTION_DELETE:
        return TransactionModalDeleteTemplateComponent;
      default:
        return null; //TODO: maybe throw Error?
    }
  }

  onSubmit(): void {
    const formData = this.templateComponentRef?.instance.getFormData?.();
    this.modalService.close({
      action: 'submit',
      data: formData
    });
    this.closeModal();
  }

  onReset(): void {
    this.templateComponentRef?.instance.resetForm?.();
  }

  onClose(): void {
    this.modalService.close({
      action: 'close'
    });
    this.closeModal();
  }

  private closeModal(): void {
    this.isOpen = false;
    this.config = null;

    if(this.templateComponentRef){
      this.templateComponentRef.destroy();
      this.templateComponentRef = null;
    }
  }

  get modalSizeClass(): string {
    const size = this.config?.size || 'md';
    return `modal-${size}`;
  }

  getButtonClass(buttonType: 'close' | 'reset' | 'submit'): string {
    const baseClass = 'btn';
    let defaultClass = '';
  
    switch (buttonType) {
      case 'close':
        defaultClass = 'btn-secondary';
        break;
      case 'reset':
        defaultClass = 'btn-warning';
        break;
      case 'submit':
        defaultClass = 'btn-primary';
        break;
    }
  
    let buttonConfig;
    switch (buttonType) {
      case 'close':
        buttonConfig = this.config?.closeButtonConfig;
        break;
      case 'reset':
        buttonConfig = this.config?.resetButtonConfig;
        break;
      case 'submit':
        buttonConfig = this.config?.submitButtonConfig;
        break;
    }
  
    if (buttonConfig?.customColor || buttonConfig?.customBackgroundColor) {
      return baseClass;
    }
  
    return `${baseClass} ${defaultClass}`;
  }
}
