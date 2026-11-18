import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Task, UpdateTask } from '../../models/task';

@Component({
  selector: 'app-edit-task-dialog',
  standalone: true,
  imports: [
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  templateUrl: './edit-task-dialog.html',
  styleUrl: './edit-task-dialog.scss'
})
export class EditTaskDialog {
  private readonly dialogRef = inject(
    MatDialogRef<EditTaskDialog, UpdateTask>
  );

  readonly task: Task = inject(MAT_DIALOG_DATA);

  form: UpdateTask = {
    title: this.task.title,
    description: this.task.description,
    status: this.task.status,
    priority: this.task.priority,
    category: this.task.category,
    dueDate: this.task.dueDate
  };

  save(): void {
    const title = this.form.title.trim();
    if (title.length < 3 || title.length > 100 ||
        (this.form.description?.length ?? 0) > 500) {
      return;
    }

    const dueDate = this.form.dueDate || null;
    this.dialogRef.close({
      ...this.form,
      title,
      description: this.form.description?.trim() || null,
      dueDate,
      clearDueDate: !dueDate && !!this.task.dueDate
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
