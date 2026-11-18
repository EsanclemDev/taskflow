import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CreateTask } from '../../models/task';

@Component({
  selector: 'app-create-task-dialog',
  standalone: true,
  imports: [
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  templateUrl: './create-task-dialog.html',
  styleUrl: './create-task-dialog.scss'
})
export class CreateTaskDialog {
  private readonly dialogRef = inject(
    MatDialogRef<CreateTaskDialog, CreateTask>
  );

  form: CreateTask = {
    title: '',
    description: null,
    priority: 'medium',
    category: 'other',
    dueDate: null
  };

  save(): void {
    const title = this.form.title.trim();
    if (title.length < 3 || title.length > 100 ||
        (this.form.description?.length ?? 0) > 500) {
      return;
    }

    this.dialogRef.close({
      ...this.form,
      title,
      description: this.form.description?.trim() || null,
      dueDate: this.form.dueDate || null
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
