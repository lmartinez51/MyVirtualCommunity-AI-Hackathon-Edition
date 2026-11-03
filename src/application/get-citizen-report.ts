import { CitizenReportRecord } from '../domain/citizen-report.js';
import { WorldCorePort } from '../ports/world-core-port.js';

export interface GetCitizenReportOptions {
  readonly worldCore: WorldCorePort;
}

export class GetCitizenReportUseCase {
  private readonly worldCore: WorldCorePort;

  constructor(options: GetCitizenReportOptions) {
    this.worldCore = options.worldCore;
  }

  public async getById(reportId: string): Promise<CitizenReportRecord | null> {
    if (typeof reportId !== 'string' || reportId.trim().length === 0) {
      return null;
    }
    if (typeof this.worldCore.getCitizenReport === 'function') {
      return this.worldCore.getCitizenReport(reportId.trim());
    }
    return null;
  }

  public async list(filter?: { limit?: number; offset?: number }): Promise<readonly CitizenReportRecord[]> {
    if (typeof this.worldCore.listCitizenReports === 'function') {
      return this.worldCore.listCitizenReports(filter);
    }
    return [];
  }
}

