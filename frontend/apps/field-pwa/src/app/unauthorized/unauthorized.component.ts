import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  imports: [RouterLink],
  template: `
    <div style="text-align:center;padding:4rem 1rem">
      <h1>Access Denied</h1>
      <p>You don't have permission to view this page.</p>
      <a routerLink="/login">Back to login</a>
    </div>
  `,
})
export class UnauthorizedComponent {}
