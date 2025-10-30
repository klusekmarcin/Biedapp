import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Title } from '@angular/platform-browser';

interface SystemInfo {
  application: string;
  version: string;
  environment: string;
  storage: {
    type: string;
    location: string;
    eventCount: number;
    isEncrypted: boolean;
  };
  system: {
    machineName: string;
    userName: string;
    osVersion: string;
    appDataFolder: string;
  };
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
  systemInfo: SystemInfo | null = null;
  loading = true;

  constructor(
    private http: HttpClient,
    title: Title) {
      title.setTitle("Settings - Biedapp");
    }

  ngOnInit(): void {
    this.loadSystemInfo();
  }

  loadSystemInfo(): void {
    this.http.get<SystemInfo>(`${environment.apiUrl}/system/info`).subscribe({
      next: (data) => {
        this.systemInfo = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading system info:', err);
        this.loading = false;
      }
    });
  }

  openDataFolder(): void {
    if (this.systemInfo?.storage.location) {
      alert(`Data location:\n${this.systemInfo.storage.location}\n\nCopy this path and open it in File Explorer.`);
    }
  }

  exportData(): void {
    this.http.get(`${environment.apiUrl}/budget/export`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `biedapp-export-${new Date().toISOString().split('T')[0]}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Error exporting data:', err);
        alert('Failed to export data');
      }
    });
  }
  
  clearData(): void {
    if (confirm('⚠️ WARNING: This will delete ALL your transactions!\n\nThis action cannot be undone. Are you absolutely sure?')) {
      this.http.delete(`${environment.apiUrl}/budget/clear-all`).subscribe({
        next: (response: any) => {
          alert(`Success! ${response.message}`);
          this.loadSystemInfo();
        },
        error: (err) => {
          console.error('Error clearing data:', err);
          alert('Failed to clear data');
        }
      });
    }
  }
}